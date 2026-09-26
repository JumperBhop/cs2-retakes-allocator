using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using static RetakesAllocatorTest.TestConstants;

namespace RetakesAllocatorTest;

public class Version301Tests : BaseTestFixture
{
    [SetUp]
    public void EnableCommonPreference() => Configs.GetConfigData().SharedSecondaryPreference = true;

    [Test]
    public async Task CommonPistolPersistsForBothTeamsAndEveryRoundWithoutReplacingLegacyValues()
    {
        await Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.Terrorist, WeaponAllocationType.Secondary, CsItem.Tec9);
        await Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.CounterTerrorist, WeaponAllocationType.PistolRound, CsItem.USPS);
        await OnWeaponCommandHelper.HandleAsync(new[] { "deagle" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, false);
        Queries.Disconnect();
        var stored = await Queries.GetUserSettings(TestSteamId);
        Assert.That(stored!.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Secondary), Is.EqualTo(CsItem.Tec9));
        Assert.That(stored.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.PistolRound), Is.EqualTo(CsItem.USPS));
        foreach (var team in new[] { CsTeam.CounterTerrorist, CsTeam.Terrorist })
        foreach (var round in Enum.GetValues<RoundType>())
            Assert.That(WeaponHelpers.GetWeaponsForRoundType(round, team, stored, false), Does.Contain(CsItem.Deagle));
    }

    [Test]
    public async Task ConcurrentMenuSavesDoNotLosePrimaryOrSharedPistol()
    {
        await Task.WhenAll(
            Queries.SetSharedSecondaryPreferenceAsync(TestSteamId, CsItem.Deagle),
            Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AK47),
            Queries.SetWeaponPreferenceForUserAsync(TestSteamId, CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary, CsItem.M4A4));
        var stored = await Queries.GetUserSettings(TestSteamId);
        Assert.That(stored!.GetSharedSecondaryPreference(), Is.EqualTo(CsItem.Deagle));
        Assert.That(stored.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.AK47));
        Assert.That(stored.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.M4A4));
    }

    [Test]
    public void LegacyConflictingPistolsHaveStableCommonResolutionAndNoDataLoss()
    {
        var stored = new UserSetting();
        stored.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Secondary, CsItem.P250);
        stored.SetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Secondary, CsItem.Deagle);
        var restored = WeaponPreferencesConverter.WeaponPreferenceDeserialize(
            WeaponPreferencesConverter.WeaponPreferenceSerialize(stored.WeaponPreferences));
        stored.WeaponPreferences = restored;
        Assert.That(stored.GetSharedSecondaryPreference(), Is.EqualTo(CsItem.Deagle));
        Assert.That(restored[CsTeam.Terrorist][WeaponAllocationType.Secondary], Is.EqualTo(CsItem.P250));
    }

    [Test]
    public async Task TeamRestrictedSharedPistolUsesConfiguredSafeFallback()
    {
        Configs.GetConfigData().SecondaryFallbackWeapons[CsTeam.CounterTerrorist] = CsItem.P250;
        await Queries.SetSharedSecondaryPreferenceAsync(TestSteamId, CsItem.Tec9);
        var stored = await Queries.GetUserSettings(TestSteamId);
        Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.Pistol, CsTeam.CounterTerrorist, stored, false), Is.EquivalentTo(new[] { CsItem.P250 }));
        Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.Pistol, CsTeam.Terrorist, stored, false), Is.EquivalentTo(new[] { CsItem.Tec9 }));
        Configs.GetConfigData().UsableWeapons.Remove(CsItem.P250);
        Assert.That(WeaponHelpers.GetSecondaryFallback(CsTeam.CounterTerrorist), Is.Not.EqualTo(CsItem.P250));
    }

    [Test]
    public async Task GroupedNoAwpBlocksMenuCommandAndStoredAllocation()
    {
        var parsed = Configs.ParseConfig("""
            {"Config":{"DatabaseProvider":"MySql","DatabaseConnectionString":"Server=existing;Database=retakes;"},
             "Weapons":{"UsableWeapons":["AWP","AK47","M4A1S","Deagle"]}, "AWP":{"EnableAwp":0}}
            """);
        Configs.OverrideConfigDataForTests(parsed);
        Assert.That(parsed.DatabaseProvider, Is.EqualTo(DatabaseProvider.MySql));
        Assert.That(parsed.DatabaseConnectionString, Is.EqualTo("Server=existing;Database=retakes;"));
        // Do not connect to the dummy MySQL endpoint: blocked input must return before touching any DB.
        var command = await OnWeaponCommandHelper.HandleAsync(new[] { "awp" }, TestSteamId, RoundType.FullBuy, CsTeam.Terrorist, false);
        Assert.That(command.Item1, Does.Contain("not allowed"));
        var stored = new UserSetting();
        stored.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred, CsItem.AWP);
        Assert.That(WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Preferred, CsTeam.Terrorist), Does.Not.Contain(CsItem.AWP));
        Assert.That(WeaponHelpers.IsWeaponAllowedForTeam(CsItem.AWP, CsTeam.Terrorist), Is.False);
        Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.FullBuy, CsTeam.Terrorist, stored, true), Does.Not.Contain(CsItem.AWP));
    }

    [Test]
    public void WeaponsWhitelistStillBlocksAwpWhenToggleIsEnabled()
    {
        Configs.GetConfigData().EnableAwp = 1;
        Configs.GetConfigData().UsableWeapons.Remove(CsItem.AWP);
        Assert.That(WeaponHelpers.IsUsableWeapon(CsItem.AWP), Is.False);
    }

    [Test]
    public void LoadingGroupedConfigNeverRewritesExistingConnectionOrStructure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "allocator-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "config"));
        var path = Path.Combine(directory, "config", "config.json");
        const string json = """
            {"Config":{"DatabaseProvider":"MySql","DatabaseConnectionString":"Server=keep;Database=retakes;"},
             "Weapons":{"UsableWeapons":["Deagle"]},"AWP":{"EnableAwp":0},"custom-key":"preserve"}
            """;
        File.WriteAllText(path, json);
        try
        {
            var loaded = Configs.Load(directory, true);
            Assert.That(loaded.DatabaseConnectionString, Is.EqualTo("Server=keep;Database=retakes;"));
            Assert.That(loaded.EnableAwp, Is.Zero);
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(Path.Combine(directory, "config"));
            Directory.Delete(directory);
        }
    }

    [Test]
    public async Task DisabledPistolCannotBeSavedAndStoredPistolCannotBeAllocated()
    {
        await Queries.SetSharedSecondaryPreferenceAsync(TestSteamId, CsItem.Deagle);
        Configs.GetConfigData().UsableWeapons.Remove(CsItem.Deagle);
        Assert.ThrowsAsync<ArgumentException>(() => Queries.SetSharedSecondaryPreferenceAsync(TestSteamId, CsItem.Deagle));
        var stored = await Queries.GetUserSettings(TestSteamId);
        Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.Pistol, CsTeam.CounterTerrorist, stored, false), Does.Not.Contain(CsItem.Deagle));
    }

    [Test]
    public void EmptyWhitelistAndSpectatorsNeverAllocateWeapons()
    {
        Configs.GetConfigData().UsableWeapons.Clear();
        foreach (var team in new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist, CsTeam.Spectator })
            Assert.That(WeaponHelpers.GetWeaponsForRoundType(RoundType.FullBuy, team, new UserSetting(), true), Is.Empty);
    }

    [Test]
    public void HeldMenuKeysDoNotRepeatAndOtherHeldButtonsDoNotBlockNewPresses()
    {
        var input = new MenuInput();
        var forward = (ulong)PlayerButtons.Forward;
        var use = (ulong)PlayerButtons.Use;
        Assert.That(input.RisingEdges(forward), Is.EqualTo(forward));
        for (var tick = 0; tick < 128; tick++) Assert.That(input.RisingEdges(forward), Is.Zero);
        Assert.That(input.RisingEdges(forward | use), Is.EqualTo(use));
        Assert.That(input.RisingEdges(forward), Is.Zero);
        Assert.That(input.RisingEdges(forward | use), Is.EqualTo(use));
        input.Initialize(use);
        Assert.That(input.RisingEdges(use), Is.Zero);
    }

    [Test]
    public void HudRefreshIsBoundedAtFourMessagesPerSecondAt64Ticks()
    {
        var gate = new HudRefreshGate();
        var sent = Enumerable.Range(0, 640).Count(tick => gate.ShouldRender(tick / 64.0));
        Assert.That(sent, Is.EqualTo(40));
        gate.Reset();
        Assert.That(gate.ShouldRender(0), Is.True);
    }

    [Test]
    public void PlantDeadlineExpiresAtTenSecondsAndRestartReplacesOldDeadline()
    {
        var timer = new PlantDeadline();
        timer.Start(100, 10);
        Assert.That(timer.Remaining(100), Is.EqualTo(10));
        Assert.That(timer.Remaining(109.99), Is.EqualTo(1));
        Assert.That(timer.Expired(109.99), Is.False);
        Assert.That(timer.Expired(110), Is.True);
        Assert.That(timer.Remaining(110), Is.Zero);
        timer.Start(110, 10);
        Assert.That(timer.Expired(110), Is.False);
        timer.Stop();
        Assert.That(timer.Expired(200), Is.False);
        Assert.That(timer.Active, Is.False);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(301)]
    public void InvalidPlantDurationIsRejected(float seconds)
    {
        Assert.Throws<Exception>(() => new ConfigData { PlantTimeSeconds = seconds }.Validate());
    }
}
