namespace RetakesAllocatorCore;

// Probe the native API before accessing CSS's exception-caching EntitySystem Lazy.
// All lifecycle operations and probes run on the server thread.
public sealed class EntityReadinessGate
{
    private bool _mapActive = true;
    private long _nextProbe;
    public bool Ready { get; private set; }

    public bool TryEnter(long nowMilliseconds, Func<bool> probe)
    {
        if (!_mapActive) return false;
        if (Ready) return true;
        if (nowMilliseconds < _nextProbe) return false;
        _nextProbe = nowMilliseconds + 1000;
        Ready = probe();
        return Ready;
    }

    public void Suspend()
    {
        _mapActive = false;
        Ready = false;
    }

    public void Resume()
    {
        _mapActive = true;
        Ready = false;
        _nextProbe = 0;
    }
}
