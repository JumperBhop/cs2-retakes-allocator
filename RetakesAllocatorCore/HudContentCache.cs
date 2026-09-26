namespace RetakesAllocatorCore;

public sealed class HudContentCache
{
    private readonly Dictionary<ulong, (string Html, double SentAt)> _last = new();
    public const int DisplaySeconds = 5;
    public bool ShouldSend(ulong player, string html, double now)
    {
        if (_last.TryGetValue(player, out var previous) && previous.Html == html && now - previous.SentAt < 2)
            return false;
        _last[player] = (html, now);
        return true;
    }
    public void Remove(ulong player) => _last.Remove(player);
    public void Reset() => _last.Clear();
}
