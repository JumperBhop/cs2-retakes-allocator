using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using static RetakesAllocatorTest.TestConstants;

namespace RetakesAllocatorTest;

public class MySqlCompatibilityTests
{
    [Test]
    public async Task ExistingMySqlRowAndTableSurviveCommonPistolUpdate()
    {
        var connectionString = Environment.GetEnvironmentVariable("ALLOCATOR_TEST_MYSQL");
        if (string.IsNullOrEmpty(connectionString)) Assert.Ignore("Dedicated MySQL test database is configured in Linux CI only.");
        var original = Configs.GetConfigData();
        Queries.Disconnect();
        try
        {
            Configs.OverrideConfigDataForTests(new ConfigData
            {
                DatabaseProvider = DatabaseProvider.MySql,
                DatabaseConnectionString = connectionString!,
                SharedSecondaryPreference = true
            });
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE IF NOT EXISTS UserSettings (UserId BIGINT NOT NULL PRIMARY KEY, WeaponPreferences TEXT NULL)";
            await create.ExecuteNonQueryAsync();
            var legacy = new UserSetting();
            legacy.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AK47);
            legacy.SetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary, CsItem.M4A4);
            legacy.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Secondary, CsItem.Tec9);
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO UserSettings (UserId,WeaponPreferences) VALUES (@id,@preferences)";
            insert.Parameters.AddWithValue("@id", TestSteamId);
            insert.Parameters.AddWithValue("@preferences", WeaponPreferencesConverter.WeaponPreferenceSerialize(legacy.WeaponPreferences));
            await insert.ExecuteNonQueryAsync();
            await Queries.SetSharedSecondaryPreferenceAsync(TestSteamId, CsItem.Deagle);
            Queries.Disconnect();
            var restored = await Queries.GetUserSettings(TestSteamId);
            Assert.That(restored!.GetSharedSecondaryPreference(), Is.EqualTo(CsItem.Deagle));
            Assert.That(restored.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.AK47));
            Assert.That(restored.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary), Is.EqualTo(CsItem.M4A4));
            Assert.That(restored.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Secondary), Is.EqualTo(CsItem.Tec9));
            await using var columns = connection.CreateCommand();
            columns.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='UserSettings'";
            Assert.That(Convert.ToInt32(await columns.ExecuteScalarAsync()), Is.EqualTo(2));
        }
        finally
        {
            Queries.Disconnect();
            Configs.OverrideConfigDataForTests(original);
        }
    }
}
