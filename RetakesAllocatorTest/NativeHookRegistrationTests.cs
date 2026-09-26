using RetakesAllocatorCore;

namespace RetakesAllocatorTest;

public class NativeHookRegistrationTests
{
    [Test]
    public void UnresolvedPointerNeverReachesNativeHook()
    {
        var calls = new List<string>();
        using var registration = New(0, calls);
        Assert.Throws<InvalidOperationException>(() => registration.Attach());
        Assert.That(calls, Is.Empty);
        registration.Dispose();
        Assert.That(calls, Is.EqualTo(new[] { "release" }));
    }

    [Test]
    public void CallbackIsReleasedOnlyAfterDetachAndDisposeIsIdempotent()
    {
        var calls = new List<string>();
        var registration = New(42, calls);
        registration.Attach();
        registration.Attach();
        Assert.That(registration.IsAttached, Is.True);
        registration.Dispose();
        registration.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(new[] { "attach", "detach", "release" }));
            Assert.That(registration.IsAttached, Is.False);
        });
        Assert.Throws<ObjectDisposedException>(() => registration.Attach());
    }

    [Test]
    public void PartialAttachFailureStillDetachesBeforeRelease()
    {
        var calls = new List<string>();
        var registration = new NativeHookRegistration(42,
            () => { calls.Add("attach"); throw new InvalidOperationException("Native hook failed"); },
            () => calls.Add("detach"), () => calls.Add("release"));
        Assert.Throws<InvalidOperationException>(() => registration.Attach());
        registration.Dispose();
        Assert.That(calls, Is.EqualTo(new[] { "attach", "detach", "release" }));
    }

    [Test]
    public void FailedDetachRetainsCallbackAndCanRetryCleanup()
    {
        var calls = new List<string>();
        bool fail = true;
        var registration = new NativeHookRegistration(42, () => calls.Add("attach"),
            () => { calls.Add("detach"); if (fail) throw new InvalidOperationException(); },
            () => calls.Add("release"));
        registration.Attach();
        Assert.Throws<InvalidOperationException>(() => registration.Dispose());
        Assert.That(calls, Does.Not.Contain("release"));
        Assert.That(registration.IsAttached, Is.False);
        fail = false;
        registration.Dispose();
        Assert.That(calls, Is.EqualTo(new[] { "attach", "detach", "detach", "release" }));
    }

    [Test]
    public void DisposalDoesNotDependOnReloadedConfiguration()
    {
        var calls = new List<string>();
        bool enabled = true;
        var registration = New(42, calls);
        if (enabled) registration.Attach();
        enabled = false;
        registration.Dispose();
        Assert.That(calls, Is.EqualTo(new[] { "attach", "detach", "release" }));
    }

    [Test]
    public void DisposingBeforeAttachNeverUnhooks()
    {
        var calls = new List<string>();
        New(42, calls).Dispose();
        Assert.That(calls, Is.EqualTo(new[] { "release" }));
    }

    private static NativeHookRegistration New(nint handle, List<string> calls) =>
        new(handle, () => calls.Add("attach"), () => calls.Add("detach"), () => calls.Add("release"));
}
