using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class BasicTest : PageTest
{
    [SetUp]
    public async Task SetUp()
    {
        // Navigate to your application hosted locally or remotely
        await Page.GotoAsync("http://localhost:5255"); // Replace with your actual application URL
    }

    [Test]
    public async Task SystemMessageShouldBePreset()
    {
        await Expect(Page.GetByText("You are the assistant of a software engineer" )).ToBeVisibleAsync();
    }
}