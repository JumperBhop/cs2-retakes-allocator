using System.Net;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocator.AdvancedMenus;

// The allocator's existing center-HUD menu, with bounded rendering and edge-based input.
public class AdvancedGunMenu
{
    private sealed class Session
    {
        public CCSPlayerController Player = null!;
        public readonly MenuInput Input = new();
        public int Page;
        public int Index;
        public CsTeam Team;
        public UserSetting? Preferences;
        public bool Saving;
        public string Status = "";
        public bool Loading = true;
    }
    private readonly Dictionary<ulong, Session> _sessions = new();
    public bool IsOpen(CCSPlayerController player) => _sessions.ContainsKey(player.SteamID);

    public void Open(CCSPlayerController player)
    {
        if (!Helpers.PlayerIsValid(player) || player.IsBot || player.IsHLTV || Helpers.GetSteamId(player) == 0) return;
        CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        var session = new Session
        {
            Player = player,
            Team = player.Team == CsTeam.Terrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist
        };
        session.Input.Initialize((ulong)player.Buttons);
        var id = player.SteamID;
        _sessions[id] = session;
        _ = Task.Run(() => LoadPreferencesAsync(id, session));
    }

    private async Task LoadPreferencesAsync(ulong id, Session session)
    {
        try
        {
            var preferences = await Queries.GetUserSettings(id);
            Server.NextFrame(() =>
            {
                if (!_sessions.TryGetValue(id, out var current) || !ReferenceEquals(current, session)) return;
                session.Preferences = preferences;
                session.Loading = false;
            });
        }
        catch (Exception error)
        {
            Log.Error($"Menu preference load failed: {error.Message}");
            Server.NextFrame(() =>
            {
                if (!_sessions.TryGetValue(id, out var current) || !ReferenceEquals(current, session)) return;
                session.Status = "Could not load preferences. Reopen !guns to retry.";
            });
        }
    }

    public void Close(CCSPlayerController player, bool clearHud = true)
    {
        if (_sessions.Remove(player.SteamID) && clearHud && Helpers.PlayerIsValid(player))
            player.PrintToCenterHtml("", 0);
    }

    public void Reset()
    {
        foreach (var session in _sessions.Values.ToArray())
            if (Helpers.PlayerIsValid(session.Player)) session.Player.PrintToCenterHtml("", 0);
        _sessions.Clear();
    }

    private static List<CsItem> Choices(Session session) => session.Page switch
    {
        1 => WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary, session.Team)
            .Concat(WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.HalfBuyPrimary, session.Team))
            .Concat(WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Preferred, session.Team))
            .Distinct().ToList(),
        2 => WeaponHelpers.GetSharedPistols().ToList(),
        _ => new()
    };

    public void OnTick()
    {
        foreach (var (id, session) in _sessions.ToArray())
        {
            var player = session.Player;
            if (!Helpers.PlayerIsValid(player) || player.Connected != PlayerConnectedState.Connected)
            {
                _sessions.Remove(id);
                continue;
            }
            var pressed = (PlayerButtons)session.Input.RisingEdges((ulong)player.Buttons);
            if ((pressed & PlayerButtons.Reload) != 0)
            {
                if (session.Page == 0) Close(player);
                else { session.Page = 0; session.Index = 0; }
                continue;
            }
            if (session.Saving || session.Loading) continue;
            var choices = Choices(session);
            var count = session.Page == 0 ? 3 : choices.Count;
            if (count == 0) { session.Index = 0; continue; }
            session.Index = Math.Clamp(session.Index, 0, count - 1);
            if ((pressed & PlayerButtons.Forward) != 0) session.Index = (session.Index + count - 1) % count;
            else if ((pressed & PlayerButtons.Back) != 0) session.Index = (session.Index + 1) % count;
            else if (session.Page == 1 && (pressed & (PlayerButtons.Moveleft | PlayerButtons.Moveright)) != 0)
            {
                session.Team = session.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                session.Index = 0;
            }
            else if ((pressed & PlayerButtons.Use) != 0)
            {
                if (session.Page == 0)
                {
                    if (session.Index == 2) Close(player);
                    else { session.Page = session.Index + 1; session.Index = 0; }
                }
                else
                {
                    session.Saving = true;
                    var weapon = choices[session.Index];
                    var page = session.Page;
                    var team = session.Team;
                    _ = Task.Run(() => SaveAsync(id, session, weapon, page, team));
                }
            }
        }
    }

    private async Task SaveAsync(ulong id, Session session, CsItem weapon, int page, CsTeam team)
    {
        string status;
        UserSetting? preferences = session.Preferences;
        try
        {
            if (!Configs.GetConfigData().CanPlayersSelectWeapons() || !WeaponHelpers.IsUsableWeapon(weapon))
                throw new InvalidOperationException("Weapon selection is disabled.");
            if (page == 2) await Queries.SetSharedSecondaryPreferenceAsync(id, weapon);
            else
            {
                var allocation = WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(null, team, weapon);
                if (allocation == null || !WeaponHelpers.IsWeaponAllowedForTeam(weapon, team))
                    throw new InvalidOperationException("Weapon is unavailable.");
                await Queries.SetWeaponPreferenceForUserAsync(id, team, allocation.Value, weapon);
            }
            status = $"Saved: {weapon.GetName()} — next Retake spawn";
            preferences = await Queries.GetUserSettings(id);
        }
        catch (Exception error)
        {
            Log.Error($"Menu preference save failed: {error.Message}");
            status = "Could not save selection.";
        }
        Server.NextFrame(() =>
        {
            if (!_sessions.TryGetValue(id, out var current) || !ReferenceEquals(current, session) ||
                !Helpers.PlayerIsValid(session.Player)) return;
            session.Preferences = preferences;
            session.Status = status;
            session.Saving = false;
        });
    }

    public string? Render(CCSPlayerController player)
    {
        if (!_sessions.TryGetValue(player.SteamID, out var session)) return null;
        static string Html(string text) => WebUtility.HtmlEncode(text);
        static string Name(CsItem? item) => item == null ? "Default" :
            WeaponHelpers.IsUsableWeapon(item.Value) ? item.Value.GetName() : "Unavailable (fallback)";
        var builder = new StringBuilder("<b>Jumper Retakes | Guns</b><br>");
        var savedPrimary = session.Preferences?.GetWeaponPreference(session.Team, WeaponAllocationType.FullBuyPrimary);
        var savedHalf = session.Preferences?.GetWeaponPreference(session.Team, WeaponAllocationType.HalfBuyPrimary);
        builder.Append($"Primary {session.Team}: {Html(Name(savedPrimary))} / HalfBuy: {Html(Name(savedHalf))}<br>");
        builder.Append($"Secondary (CT + T): {Html(Name(session.Preferences?.GetSharedSecondaryPreference()))}<br>");
        var rows = session.Page == 0 ? new List<string> { "Primary Weapon", "Secondary Weapon", "Close" }
            : Choices(session).Select(weapon => weapon.GetName()).ToList();
        if (rows.Count == 0) builder.Append("No enabled weapons<br>");
        session.Index = rows.Count == 0 ? 0 : Math.Clamp(session.Index, 0, rows.Count - 1);
        var start = Math.Max(0, Math.Min(session.Index - 2, rows.Count - 5));
        for (var i = start; i < Math.Min(rows.Count, start + 5); i++)
            builder.Append(i == session.Index ? $"<font color='orange'>&gt; {Html(rows[i])}</font><br>" : $"{Html(rows[i])}<br>");
        builder.Append("W/S: navigate | E: select | R: back/close<br>");
        if (session.Page == 1) builder.Append("A/D: switch CT / T<br>");
        builder.Append(Html(session.Saving ? "Saving..." : session.Loading && session.Status == "" ? "Loading..." : session.Status));
        builder.Append($"<br><b>{Html(Configs.GetConfigData().MenuFooter)}</b>");
        return builder.ToString();
    }
}
