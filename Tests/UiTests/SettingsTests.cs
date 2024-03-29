using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SettingsTests : PageTest
{
    [SetUp]
    public async Task SetUp()
    {
        await Page.GotoAsync(BasicTest.BaseUrl);
    }

    [Test]
    public async Task LocalStorageShouldHaveUpdatedValueAfterChanges()
    {
        // Wait for submit button to be enabled
        await Page.WaitForSelectorAsync("#searchBtn:not([disabled])", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        var storedString = await Page.EvaluateAsync<string>("() => localStorage.getItem('IsAutoscrollEnabled')");

        Assert.IsTrue(Boolean.TryParse(storedString, out var storedBefore), $"{storedString} expected to be a valid bool");

        await Page.ClickAsync("#isAutoscrollEnabled");

        await Page.WaitForFunctionAsync($"() => localStorage.getItem('IsAutoscrollEnabled') !== '{storedString}'");

        storedString = await Page.EvaluateAsync<string>("() => localStorage.getItem('IsAutoscrollEnabled')");

        Assert.IsTrue(Boolean.TryParse(storedString, out var storedAfter), $"{storedString} expected to be a valid bool");

        Assert.AreNotEqual(storedBefore, storedAfter, "expected changed value");
    }
}