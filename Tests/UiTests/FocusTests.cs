using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class FocusTests : PageTest
{
    [SetUp]
    public async Task SetUp()
    {
        // Navigate to your application hosted locally or remotely
        await Page.GotoAsync("http://localhost:5255"); // Replace with your actual application URL
    }

    [Test]
    public async Task NextAreaShouldHaveFocusAfterLoading()
    {
        // Wait for submit button to be enabled
        await Page.WaitForSelectorAsync("#searchBtn:not([disabled])", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        // Check if the textarea with id="nextArea" is focused after loading disappears.
        var isNextAreaFocused = await Page.EvaluateAsync<bool>("document.activeElement.id === 'nextArea'");

        Assert.IsTrue(isNextAreaFocused, "Expected nextArea to have focus after loading is finished.");
    }
}