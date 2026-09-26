using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using RetakesAllocatorCore;

namespace RetakesAllocator;

// Workaround by Poggu, documented in girlglock/CS2FlashingHtmlHudFix (GPL-3.0).
// This implementation acquires the flag only for our HUD and never writes RestartRoundTime.
public sealed class HtmlHudStability
{
    private readonly HtmlHudRestartLease _lease = new();
    private CCSGameRules? _rules;

    public void Update(bool visible)
    {
        if (!Helpers.EntityLifecycle.Ready || (!visible && _rules == null)) return;
        var current = Helpers.GetGameRules();
        if (current == null) return;
        if (_rules?.Handle != current.Handle) { _lease.Forget(); _rules = current; }
        var flag = _lease.Update(visible, current.GameRestart, current.RestartRoundTime, Server.CurrentTime);
        if (flag != current.GameRestart) current.GameRestart = flag;
    }

    public void Reset()
    {
        Update(false);
        _lease.Forget();
        _rules = null;
    }
}
