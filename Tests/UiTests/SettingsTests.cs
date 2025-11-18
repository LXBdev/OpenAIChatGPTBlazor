using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace UiTests;

/// <remarks>
/// Tests are slightly flaky because Settings are stored and loaded in additional render step because of JS interop
/// </remarks>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SettingsTests : PageTest
{
    [SetUp]
    public async Task SetUp()
    {
        await Page.GotoAsync(BasicTest.BaseUrl);
        await Page.WaitForSelectorAsync(
            "#searchBtn:not([disabled])",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible }
        );
    }

    [Test]
    public async Task LocalStorageShouldHaveUpdatedValueAfterChanges()
    {
        // Given
        var checkedBefore = await Page.IsCheckedAsync("#isAutoscrollEnabled");
        Assert.IsTrue(checkedBefore, "Autoscroll should be enabled by default");

        // When
        await Page.ClickAsync("#isAutoscrollEnabled");

        await Page.WaitForFunctionAsync(
            "() => localStorage.getItem('IsAutoscrollEnabled') === 'false'"
        );

        // Then
        var storedString = await Page.EvaluateAsync<string>(
            "() => localStorage.getItem('IsAutoscrollEnabled')"
        );
        Assert.IsTrue(
            bool.TryParse(storedString, out var storedAfter),
            $"{storedString} expected to be a valid bool"
        );
        Assert.AreNotEqual(checkedBefore, storedAfter, "expected changed value");
    }

    [Test]
    public async Task ChangedSettingShouldUseStoredValueOnPageLoad()
    {
        // Given
        var isChecked = await Page.IsCheckedAsync("#isAutoscrollEnabled");
        Assert.IsTrue(isChecked, "Autoscroll should be enabled by default");

        // When setting is changed and page reloaded
        await Page.ClickAsync("#isAutoscrollEnabled");
        await Page.WaitForFunctionAsync(
            "() => localStorage.getItem('IsAutoscrollEnabled') === 'false'"
        );
        await Page.ReloadAsync();

        await Page.WaitForSelectorAsync(
            "#searchBtn:not([disabled])",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible }
        );

        // Then
        isChecked = await Page.IsCheckedAsync("#isAutoscrollEnabled");
        Assert.IsFalse(
            isChecked,
            "Autoscroll should be disabled after reload because the setting should be persisted"
        );
    }
}
