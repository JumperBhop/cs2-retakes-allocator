using System.Net;
using System.Text;

namespace RetakesAllocatorCore;

public static class GunMenuView
{
    public static IReadOnlyList<string> MainOptions { get; } = Array.AsReadOnly(new[] { "Primary Weapon", "Secondary Weapon" });
    public static string Render(int page, int selected, IReadOnlyList<string> weapons, string primary,
        string secondary, string team, string status, string footer)
    {
        static string Html(string value) => WebUtility.HtmlEncode(value);
        var rows = page == 0 ? MainOptions : weapons;
        selected = rows.Count == 0 ? 0 : Math.Clamp(selected, 0, rows.Count - 1);
        var builder = new StringBuilder("<font class='fontSize-l' color='#FFFFFF'><b>Jumper Retakes | Guns</b></font><br>");
        if (page != 0)
            builder.Append($"<font class='fontSize-m' color='#FFFFFF'>{(page == 1 ? "Primary Weapon" : "Secondary Weapon")}: {Html(page == 1 ? primary : secondary)}</font><br>");
        if (rows.Count == 0) builder.Append("<font class='fontSize-l'>No enabled weapons</font><br>");
        var start = page == 0 ? 0 : Math.Max(0, Math.Min(selected - 1, rows.Count - 3));
        for (var i = start; i < Math.Min(rows.Count, start + 3); i++)
        {
            var color = i == selected ? "#FFD166" : "#FFFFFF";
            builder.Append($"<font class='fontSize-l' color='{color}'><b>{Html(rows[i])}</b></font><br>");
            if (page == 0)
                builder.Append($"<font class='fontSize-m' color='#BBBBBB'>{Html(i == 0 ? primary : secondary)}</font><br>");
        }
        builder.Append("<font class='fontSize-m' color='#CCCCCC'>W/S: navigate | E: select | R: back/close</font><br>");
        if (page == 1) builder.Append($"<font class='fontSize-m' color='#CCCCCC'>A/D: CT / T | {Html(team)}</font><br>");
        if (!string.IsNullOrEmpty(status)) builder.Append($"<font class='fontSize-m' color='#CCCCCC'>{Html(status)}</font><br>");
        builder.Append($"<font class='fontSize-m' color='#FFFFFF'><b>{Html(footer)}</b></font>");
        return builder.ToString();
    }
}
