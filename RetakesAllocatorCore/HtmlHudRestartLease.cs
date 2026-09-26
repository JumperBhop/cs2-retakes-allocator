namespace RetakesAllocatorCore;

// Scope the known CS2 HTML-panel workaround to owned HUD output, respecting scheduled restarts.
public sealed class HtmlHudRestartLease
{
    private bool _owned;
    private float _restartAt;
    public bool Update(bool showHud, bool gameRestart, float restartAt, float now)
    {
        if (_owned && restartAt != _restartAt) _owned = false;
        if (!showHud || restartAt >= now)
        {
            if (_owned) { _owned = false; return false; }
            return gameRestart;
        }
        if (!gameRestart)
        {
            _owned = true;
            _restartAt = restartAt;
            return true;
        }
        return gameRestart;
    }
    public void Forget() => _owned = false;
}
