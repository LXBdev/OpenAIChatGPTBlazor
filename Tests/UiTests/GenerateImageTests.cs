using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class GenerateImageTests : PageTest
{
    [SetUp]
    public async Task SetUp()
    {
        await Page.GotoAsync(Path.Combine(BasicTest.BaseUrl, "GenerateImage"));
    }

    [Test]
    public async Task PageShouldLoadAndShowHeading()
    {
        await Expect(Page.GetByText("Welcome to my Image Generation using OpenAI"))
            .ToBeVisibleAsync();
    }
}
