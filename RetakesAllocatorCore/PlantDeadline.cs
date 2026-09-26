namespace RetakesAllocatorCore;

// One deadline per active round; based on server simulation time, not wall time.
public sealed class PlantDeadline
{
    public bool Active { get; private set; }
    private double _deadline;
    public void Start(double now, double seconds) { _deadline = now + seconds; Active = true; }
    public void Stop() => Active = false;
    public int Remaining(double now) => Active ? Math.Max(0, (int)Math.Ceiling(_deadline - now)) : 0;
    public bool Expired(double now) => Active && now >= _deadline;
}
