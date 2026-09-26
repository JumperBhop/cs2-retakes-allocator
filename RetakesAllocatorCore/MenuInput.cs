namespace RetakesAllocatorCore;

public sealed class MenuInput
{
    private ulong _previous;
    public ulong RisingEdges(ulong current)
    {
        var pressed = current & ~_previous;
        _previous = current;
        return pressed;
    }
    public void Initialize(ulong current) => _previous = current;
}
