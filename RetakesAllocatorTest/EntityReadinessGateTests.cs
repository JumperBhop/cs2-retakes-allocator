using RetakesAllocatorCore;

namespace RetakesAllocatorTest;

public class EntityReadinessGateTests
{
    [Test]
    public void StartupRetriesWithoutCallingEntityConsumersUntilNativeReady()
    {
        var gate = new EntityReadinessGate();
        var nativeReady = false;
        var probes = 0;
        var entityReads = 0;
        bool Probe() { probes++; return nativeReady; }
        void Tick(long now) { if (gate.TryEnter(now, Probe)) entityReads++; }

        Tick(0);
        Tick(10);
        Tick(999);
        Assert.That(entityReads, Is.Zero);
        Assert.That(probes, Is.EqualTo(1));
        nativeReady = true;
        Tick(1000);
        Tick(1010);
        Assert.That(entityReads, Is.EqualTo(2));
        Assert.That(probes, Is.EqualTo(2));
    }

    [Test]
    public void MapEndBlocksAllProbesAndNewMapMustBecomeReadyAgain()
    {
        var gate = new EntityReadinessGate();
        Assert.That(gate.TryEnter(0, () => true), Is.True);
        gate.Suspend();
        Assert.That(gate.Ready, Is.False);
        Assert.That(gate.TryEnter(5000, () => throw new AssertionException("Probe called during map teardown")), Is.False);
        gate.Resume();
        Assert.That(gate.TryEnter(5001, () => false), Is.False);
        Assert.That(gate.TryEnter(6001, () => true), Is.True);
    }

    [Test]
    public void HotReloadCanEnterAnAlreadyReadyServerWithoutWaitingForMapStart()
    {
        var gate = new EntityReadinessGate();
        Assert.That(gate.Ready, Is.False);
        Assert.That(gate.TryEnter(12345, () => true), Is.True);
    }

    [Test]
    public void UnexpectedProbeErrorsAreNotSilencedOrCached()
    {
        var gate = new EntityReadinessGate();
        Assert.Throws<InvalidOperationException>(() => gate.TryEnter(0, () => throw new InvalidOperationException("Unexpected")));
        Assert.That(gate.Ready, Is.False);
        Assert.That(gate.TryEnter(1000, () => true), Is.True);
    }
}
