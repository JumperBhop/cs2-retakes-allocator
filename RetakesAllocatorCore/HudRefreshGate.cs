namespace RetakesAllocatorCore;

public sealed class HudRefreshGate
{
    private double _next;
    public bool ShouldRender(double now)
    {
        if (now < _next) return false;
        _next = now + 0.25;
        return true;
    }
    public void Reset() => _next = 0;
}
