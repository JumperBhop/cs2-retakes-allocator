using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator;

public sealed class PlantTimerController
{
    private readonly RetakesAllocator _plugin;
    private readonly PlantDeadline _deadline = new();
    private Timer? _timer;
    private bool _started;
    private bool _planted;
    private bool _canTerminate;
    private float _timeoutMessageUntil;
    public PlantTimerController(RetakesAllocator plugin) => _plugin = plugin;

    public void Initialize()
    {
        if (!Configs.GetConfigData().PlantTimerEnabled) return;
        // The public CSS375 TerminateRound API uses these canonical handles internally.
        try
        {
            _canTerminate = (OperatingSystem.IsLinux() ? VirtualFunctions.TerminateRoundFuncLinux.Handle :
                VirtualFunctions.TerminateRoundFuncWindows.Handle) != IntPtr.Zero;
        }
        catch (Exception error) { Log.Error($"Plant timer TerminateRound resolution failed: {error.Message}"); }
        if (!_canTerminate && Configs.GetConfigData().PlantTimerEnabled)
            Log.Error("Plant timer disabled: CSS375 TerminateRound signature unavailable. Update complete CSS gamedata.");
    }

    public void NewRound()
    {
        Stop();
        _started = false;
        _planted = false;
        _timeoutMessageUntil = 0;
    }

    public void OnPlant() { _planted = true; Stop(); }
    public void Stop()
    {
        _deadline.Stop();
        _timer?.Kill();
        _timer = null;
    }

    public void Start(bool activeRetake)
    {
        var config = Configs.GetConfigData();
        var rules = Helpers.GetGameRules();
        if (!activeRetake || _started || _planted || !_canTerminate || !config.PlantTimerEnabled ||
            rules == null || rules.WarmupPeriod || rules.FreezePeriod || rules.BombPlanted) return;
        if (!Utilities.GetPlayers().Any(p => Helpers.PlayerCanReceiveWeapons(p) && p.Team == CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist) ||
            !Utilities.GetPlayers().Any(p => Helpers.PlayerCanReceiveWeapons(p) && p.Team == CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist)) return;
        Stop();
        _started = true;
        _deadline.Start(Server.CurrentTime, config.PlantTimeSeconds);
        _timer = _plugin.AddTimer(config.PlantTimeSeconds, Expire, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void Expire()
    {
        _timer = null;
        var rules = Helpers.GetGameRules();
        if (!_deadline.Active || !Configs.GetConfigData().PlantTimerEnabled || _planted ||
            rules == null || rules.WarmupPeriod || rules.BombPlanted)
        {
            Stop();
            return;
        }
        Stop();
        try
        {
            rules.TerminateRound(0.1f, RoundEndReason.CTsWin);
            _timeoutMessageUntil = Server.CurrentTime + 2;
            Server.PrintToChatAll($"{PluginInfo.MessagePrefix}Plant time expired — CTs win.");
        }
        catch (Exception error)
        {
            _canTerminate = false;
            Log.Error($"Could not end round via CSS375 TerminateRound: {error.Message}. Plant timer disabled until restart.");
        }
    }

    public string? Render()
    {
        if (_deadline.Active && (!Configs.GetConfigData().PlantTimerEnabled || Helpers.GetGameRules()?.BombPlanted == true)) Stop();
        if (_timeoutMessageUntil > Server.CurrentTime) return "<b>Plant the bomb: 0s — CTs win</b>";
        return _deadline.Active && Configs.GetConfigData().ShowPlantCountdown
            ? $"<b>Plant the bomb: {_deadline.Remaining(Server.CurrentTime)}s</b>" : null;
    }
}
