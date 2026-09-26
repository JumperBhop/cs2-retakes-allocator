using CounterStrikeSharp.API.Core.Translations;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void Setup()
    {
        var english = System.Globalization.CultureInfo.GetCultureInfo("en");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = english;
        System.Globalization.CultureInfo.CurrentUICulture = english;
        Configs.Load(".", true);
        Queries.Migrate();
        Translator.Initialize(new JsonStringLocalizer(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../RetakesAllocator/lang"))));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Queries.Disconnect();
    }
}

public abstract class BaseTestFixture
{
    [SetUp]
    public void GlobalSetup()
    {
        Configs.Load(".");
        // Retain upstream behavior coverage; v301 tests explicitly enable the common preference.
        Configs.GetConfigData().SharedSecondaryPreference = false;
        Queries.Wipe();
    }
}
