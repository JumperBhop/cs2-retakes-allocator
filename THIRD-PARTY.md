# Dependencies and source

This is a GPL-3.0 fork of yonilerner/cs2-retakes-allocator v2.4.2.
Original author and contributor attribution is retained.
Source: https://github.com/JumperBhop/cs2-retakes-allocator/tree/jumper/v301

Compile-time server contracts (not distributed in this plugin ZIP):
- CounterStrikeSharp.API 1.0.375, GPL-3.0
- RetakesPluginShared 2.0.0, GPL-3.0

The existing Entity Framework 7 database code and migration schema are retained.
Distributed dependencies include Entity Framework / Microsoft.Data.Sqlite 7.0.14 (MIT),
MySqlConnector 2.3.4 (MIT), Pomelo.EntityFrameworkCore.MySql 7.0.0 (MIT),
and SQLitePCLRaw 2.1.13 (Apache-2.0) with native SQLite (public domain).
Versions and complete runtime dependencies are recorded in RetakesAllocator.deps.json.
The SQLitePCLRaw dependency was updated from 2.1.7 to 2.1.13 without changing
the application's database schema.

HTML HUD stabilization uses the GameRestart / RestartRoundTime workaround discovered by Poggu and documented by Deana (girlglock) in CS2FlashingHtmlHudFix (GPL-3.0): https://github.com/girlglock/CS2FlashingHtmlHudFix/blob/main/src/FlashingHtmlHudFix.cs . This fork implements an owned, temporary flag lease with cleanup and scheduled-restart guards; no additional plugin or library is distributed.
