using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class BasicTest : PageTest
{
    public static string BaseUrl = Environment.GetEnvironmentVariable("AppUrl") ?? "https://localhost:7128/";

    [SetUp]
    public async Task SetUp()
    {
        await Page.GotoAsync(BaseUrl);
    }

    [Test]
    public async Task SystemMessageShouldBePreset()
    {
        await Expect(Page.GetByText("You are the assistant of a software engineer" )).ToBeVisibleAsync();
    }
}