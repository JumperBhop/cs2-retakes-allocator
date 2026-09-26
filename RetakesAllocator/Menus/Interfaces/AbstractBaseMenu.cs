using CounterStrikeSharp.API.Core;

namespace RetakesAllocator.Menus.Interfaces;

public abstract class AbstractBaseMenu
{
    protected const float MenuTimeout = 30.0f;

    protected readonly HashSet<CCSPlayerController> PlayersInMenu = new();

    public virtual void Reset()
    {
        foreach (var player in PlayersInMenu)
            if (Helpers.PlayerIsValid(player)) CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        PlayersInMenu.Clear();
    }

    public abstract void OpenMenu(CCSPlayerController player);
    public abstract bool PlayerIsInMenu(CCSPlayerController player);
}
