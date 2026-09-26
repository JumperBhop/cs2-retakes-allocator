using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Managers;
using static RetakesAllocatorTest.TestConstants;

namespace RetakesAllocatorTest;

public class CompatibilityRegressionTests : BaseTestFixture
{
    [Test]
    public async Task NextSpawnSelectionCommitsBeforeReturningWithoutImmediateWeapon()
    {
        Configs.GetConfigData().ApplySelectionsOnNextSpawnOnly = true;
        var result = await OnWeaponCommandHelper.HandleAsync(
            new[] { "ak" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, false);
        Assert.That(result.Item2, Is.Null);
        Queries.Disconnect();
        var stored = await Queries.GetUserSettings(TestSteamId);
        Assert.That(stored?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.AK47));
    }

    [Test]
    public async Task TeamPrimaryAndSecondaryPersistAndFeedFollowingAllocations()
    {
        Configs.GetConfigData().ApplySelectionsOnNextSpawnOnly = true;
        await OnWeaponCommandHelper.HandleAsync(new[] { "ak" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, false);
        await OnWeaponCommandHelper.HandleAsync(new[] { "tec9" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, false);
        await OnWeaponCommandHelper.HandleAsync(new[] { "m4a1s" }, TestSteamId, RoundType.FullBuy, CsTeam.CounterTerrorist, false);
        await OnWeaponCommandHelper.HandleAsync(new[] { "deagle" }, TestSteamId, RoundType.FullBuy, CsTeam.CounterTerrorist, false);
        Queries.Disconnect();
        var stored = await Queries.GetUserSettings(TestSteamId);
        Assert.Multiple(() =>
        {
            Assert.That(stored?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.AK47));
            Assert.That(stored?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Secondary), Is.EqualTo(CsItem.Tec9));
            Assert.That(stored?.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.M4A1S));
            Assert.That(stored?.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Secondary), Is.EqualTo(CsItem.Deagle));
        });
        for (int round = 0; round < 3; round++)
        {
            Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.FullBuy, CsTeam.Terrorist, stored, false),
                Is.EquivalentTo(new[] { CsItem.AK47, CsItem.Tec9 }));
            Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.FullBuy, CsTeam.CounterTerrorist, stored, false),
                Is.EquivalentTo(new[] { CsItem.M4A1S, CsItem.Deagle }));
        }
    }

    [Test]
    public async Task RemovingPreferenceIsCommittedBeforeReturn()
    {
        await OnWeaponCommandHelper.HandleAsync(new[] { "ak" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, false);
        await OnWeaponCommandHelper.HandleAsync(new[] { "ak" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, true);
        Queries.Disconnect();
        Assert.That((await Queries.GetUserSettings(TestSteamId))?.GetWeaponPreference(
            CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary), Is.Null);
    }
}
