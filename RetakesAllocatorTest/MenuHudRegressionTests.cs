using RetakesAllocatorCore;

namespace RetakesAllocatorTest;

public class MenuHudRegressionTests
{
    [Test]
    public void MainMenuHasOnlyPrimaryAndSecondaryWithLargeLabelsAndFooter()
    {
        Assert.That(GunMenuView.MainOptions, Is.EqualTo(new[] { "Primary Weapon", "Secondary Weapon" }));
        var html = GunMenuView.Render(0, 0, Array.Empty<string>(), "AK-47", "Desert Eagle", "T", "", "by Jumper");
        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("fontSize-l"));
            Assert.That(html, Does.Contain("Primary Weapon"));
            Assert.That(html, Does.Contain("Secondary Weapon"));
            Assert.That(html, Does.Contain("AK-47"));
            Assert.That(html, Does.Contain("Desert Eagle"));
            Assert.That(html, Does.Contain("by Jumper"));
            Assert.That(html, Does.Not.Contain("Loadout"));
            Assert.That(html, Does.Not.Contain("HalfBuy"));
            Assert.That(html, Does.Not.Contain("Pistol Round"));
            Assert.That(html, Does.Not.Contain("Close"));
            Assert.That(html, Does.Not.Contain("█"));
            Assert.That(html, Does.Not.Contain("<img"));
        });
    }

    [Test]
    public void SubmenuShowsThreeLargeWeaponRowsAndEscapesUserText()
    {
        var weapons = new[] { "AK-47", "M4A4", "M4A1-S", "FAMAS", "Galil" };
        var html = GunMenuView.Render(1, 3, weapons, "AK-47", "P250", "CT", "<img src='bad'>", "by Jumper");
        Assert.That(html, Does.Contain("M4A1-S"));
        Assert.That(html, Does.Contain("FAMAS"));
        Assert.That(html, Does.Contain("Galil"));
        Assert.That(html, Does.Not.Contain("<b>M4A4</b>"));
        Assert.That(html, Does.Not.Contain("<img"));
        Assert.That(html, Does.Contain("by Jumper"));
    }

    [Test]
    public void UnchangedHudIsSentOnlyEveryTwoSecondsAndBeforeDisplayExpires()
    {
        var cache = new HudContentCache();
        var sends = Enumerable.Range(0, 40).Count(tick => cache.ShouldSend(1, "same", tick * 0.25));
        Assert.That(sends, Is.EqualTo(5));
        Assert.That(HudContentCache.DisplaySeconds, Is.GreaterThan(2));
        Assert.That(cache.ShouldSend(1, "changed", 9.8), Is.True);
        Assert.That(cache.ShouldSend(2, "changed", 9.8), Is.True);
        cache.Remove(1);
        Assert.That(cache.ShouldSend(1, "changed", 9.9), Is.True);
        cache.Reset();
        Assert.That(cache.ShouldSend(2, "changed", 0), Is.True);
    }

    [Test]
    public void HudRestartFlagIsOwnedOnlyWhileShowingHudAndRestoredOnClose()
    {
        var lease = new HtmlHudRestartLease();
        Assert.That(lease.Update(false, false, 0, 10), Is.False);
        Assert.That(lease.Update(true, false, 0, 10), Is.True);
        Assert.That(lease.Update(true, true, 0, 11), Is.True);
        Assert.That(lease.Update(false, true, 0, 12), Is.False);
        Assert.That(lease.Update(false, false, 0, 13), Is.False);
    }

    [Test]
    public void ExistingFlagAndScheduledRestartAreNeverOverwritten()
    {
        var lease = new HtmlHudRestartLease();
        Assert.That(lease.Update(true, true, 0, 10), Is.True);
        Assert.That(lease.Update(false, true, 0, 11), Is.True);
        Assert.That(lease.Update(true, false, 20, 10), Is.False);
        Assert.That(lease.Update(true, true, 20, 10), Is.True);
        lease.Forget();
        Assert.That(lease.Update(true, false, 0, 10), Is.True);
        Assert.That(lease.Update(false, true, 20, 11), Is.True);
    }
}
