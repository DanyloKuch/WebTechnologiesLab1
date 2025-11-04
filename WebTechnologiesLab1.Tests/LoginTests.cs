using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Threading.Tasks;

namespace WebTechnologiesLab1.Tests
{
    [TestClass]
    public class LoginTests : PageTest
    {
        private const string WebsiteUrl = "https://labwebtech-ewhtcaavgufjh2h9.polandcentral-01.azurewebsites.net/";

        [TestMethod]
        public async Task Should_Successfully_Login_With_Valid_Credentials()
        {
            await Page.GotoAsync(WebsiteUrl);

            await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();

            await Page.GetByLabel("Email").FillAsync("123@gmail.com");
            await Page.GetByLabel("Password").FillAsync("Qwer1234%");

            await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

            var greetingLocator = Page.GetByText($"Hello 123@gmail.com!");

            await Assertions.Expect(greetingLocator).ToBeVisibleAsync();
        }
    }
}