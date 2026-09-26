> **Jumper compatibility fork:** based on v2.4.2, updated for CounterStrikeSharp 375 / Retakes 3.0.2 / CS2 1.41.8.2. Read [INSTALLATION-DE.md](INSTALLATION-DE.md) before upgrading. Existing database and weapon preferences are retained.
# CS2 Retakes Allocator

[![Build RetakesAllocator.zip](https://github.com/yonilerner/cs2-retakes-allocator/actions/workflows/build.yml/badge.svg)](https://github.com/yonilerner/cs2-retakes-allocator/actions/workflows/build.yml)

## Retakes

This plugin is made to run alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Installation

- Ensure you have https://github.com/b3none/cs2-retakes installed already
- Update the `RetakesPlugin` config to have `EnableFallbackAllocation` disabled
- Download a release from https://github.com/yonilerner/cs2-retakes-allocator/releases
- Extract the zip archive and upload the `RetakesAllocator` plugin to your CounterStrikeSharp plugins folder on your
  server
    - Each build comes with two necessary runtimes for sqlite3, one for linux64 and one for win64. If you need a
      different runtime, please submit an issue and I can provide more runtimes
    - If you're wondering why so many DLLs in the build: They are necessary for the Entity Framework that enables modern
      interfaces for databases
- [Buy Menu - Optional] If you want buy menu weapon selection to work, ensure the following convars are set at the
  bottom of `game/csgo/cfg/cs2-retakes/retakes.cfg`:
    - `mp_buy_anywhere 1`
    - `mp_buytime 60000`
    - `mp_maxmoney 65535`
    - `mp_startmoney 65535`
    - `mp_afterroundmoney 65535`
    - More info about this in the "Buy Menu" section below

## Game Data and native hooks in this fork

CounterStrikeSharp 375's installed gamedata is the only source of CanAcquire and weapon-data signatures. Old plugin-local gamedata and its background updater are no longer used. Every function handle is checked before hooking. Callback lifetime is explicitly managed until native detachment succeeds.

Weapon allocation uses the supported CSSharp GiveNamedItem API. The custom GiveNamedItem2 path has been removed. Special cross-team weapon paint preservation is no longer guaranteed.

AutoUpdateSignatures and CapabilityWeaponPaints remain readable legacy config keys, but are ignored. EnableCanAcquireHook controls the optional buy-menu hook. If it cannot be resolved, !guns and regular round allocation remain available. With ApplySelectionsOnNextSpawnOnly=true (default), purchases outside warmup are blocked and weapon commands only save future preferences.
### Commands

You can use the following commands to select specific weapon preferences per-user:

- `!gun <weapon> [T|CT]` - Set a preference the chosen weapon for the team you are currently on, or T/CT if provided
    - For example, if you are currently a terrorist and you do `!gun galil`, your preference for rifle rounds will be
      Galil
- `!guns` - Opens up a chat-based menu for setting weapon preferences.
- `!awp` - Toggle whether or not you want to get an AWP.
- `!removegun <weapon> [T|CT]` - Remove a preference for the chosen weapon for the team you are currently on, or T/CT if
  provided
    - For example, if you previously did `!gun galil` while a terrorist, and you do `!removegun galil` while a
      terrorist, you will no longer prefer the galil, and will instead get a random weapon
- `!nextround` - Vote for the next round type. Can be enabled with the `EnableNextRoundTypeVoting` config, which
  is `false` by default.
- `!setnextround <P|H|F>` - For admins only. Force the next round to be the selected type.
- `!reload_allocator_config` - For admins only. Reload the JSON config in-place.
- `!print_config <name>` - For admins only. Print out the config with the given name.

# Building

Use .NET 10 SDK. Build with dotnet build -c Release, run dotnet test, and package with pwsh -File scripts/package.ps1.

To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to the folder
where the mod should be copied to. *This only works on Windows.*

Notes:

- Run the dedicated server
  with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`
