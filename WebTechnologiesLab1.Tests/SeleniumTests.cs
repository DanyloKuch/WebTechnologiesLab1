using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace WebTechnologiesLab1.Tests;

public class SeleniumTests : IClassFixture<WebHostFixture>, IDisposable
{
    private const string BaseUrl = "https://localhost:7062";
    private const string TestEmail = "selenium-test@example.com";
    private const string TestPassword = "Selenium123!";
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public SeleniumTests(WebHostFixture _)
    {
        var options = new ChromeOptions
        {
            AcceptInsecureCertificates = true
        };
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--allow-insecure-localhost");
        options.AddArgument("--start-maximized");

        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    public void Dispose()
    {
        _driver.Quit();
    }

    private void EnsureLoggedIn()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");

        var emailInput = _wait.Until(d => d.FindElement(By.Id("Input_Email")));
        emailInput.Clear();
        emailInput.SendKeys(TestEmail);

        var passwordInput = _driver.FindElement(By.Id("Input_Password"));
        passwordInput.Clear();
        passwordInput.SendKeys(TestPassword);

        _driver.FindElement(By.Id("login-submit")).Click();

        try
        {
            _wait.Until(d => !d.Url.Contains("/Identity/Account/Login"));
            return;
        }
        catch (WebDriverTimeoutException)
        {
        }

        _driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Register");

        var regEmail = _wait.Until(d => d.FindElement(By.Id("Input_Email")));
        regEmail.Clear();
        regEmail.SendKeys(TestEmail);

        var regPassword = _driver.FindElement(By.Id("Input_Password"));
        regPassword.Clear();
        regPassword.SendKeys(TestPassword);

        var regConfirm = _driver.FindElement(By.Id("Input_ConfirmPassword"));
        regConfirm.Clear();
        regConfirm.SendKeys(TestPassword);

        _driver.FindElement(By.Id("registerSubmit")).Click();

        _wait.Until(d => !d.Url.Contains("/Identity/Account/Register"));
    }

    [Fact]
    public void HomePageCatalogButton_NavigatesToProductsPage()
    {
    
       EnsureLoggedIn();
        _driver.Navigate().GoToUrl(BaseUrl);

        var catalogButton = _wait.Until(d =>
            d.FindElement(By.CssSelector("a.btn-primary[href*='Products']")));
        catalogButton.Click();

        _wait.Until(d => d.Url.Contains("/Products"));
        Assert.Contains("/Products", _driver.Url);

        var heading = _wait.Until(d =>
            d.FindElement(By.CssSelector("h1")));
        Assert.Contains("Каталог Товарів", heading.Text);
    }

    [Fact]
    public void LoginForm_InvalidCredentials_ShowsErrorMessage()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");

        var emailInput = _wait.Until(d =>
            d.FindElement(By.Id("Input_Email")));
        emailInput.Clear();
        emailInput.SendKeys("nonexistent@test.com");

        var passwordInput = _driver.FindElement(By.Id("Input_Password"));
        passwordInput.Clear();
        passwordInput.SendKeys("WrongPassword123!");

        var submitButton = _driver.FindElement(By.Id("login-submit"));
        submitButton.Click();

        _wait.Until(d => d.Url.Contains("Login"));
        Assert.Contains("Login", _driver.Url);

        var errorSummary = _wait.Until<IWebElement?>(d =>
        {
            var el = d.FindElement(By.CssSelector("div[role='alert']"));
            return !string.IsNullOrWhiteSpace(el.Text) ? el : null;
        });
        Assert.NotNull(errorSummary);
        Assert.Contains("Invalid", errorSummary!.Text);
    }

    [Fact]
    public void CategoriesPage_TableVisible_AndCreateFormOpens()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Categories");

        var nameHeader = _wait.Until(d =>
            d.FindElement(By.XPath("//th[normalize-space()='Name']")));
        Assert.True(nameHeader.Displayed);

        var descHeader = _driver.FindElement(
            By.XPath("//th[normalize-space()='Description']"));
        Assert.True(descHeader.Displayed);

        var createLink = _driver.FindElement(By.LinkText("Create New"));
        createLink.Click();

        _wait.Until(d => d.Url.Contains("/Categories/Create"));
        Assert.Contains("/Categories/Create", _driver.Url);

        var nameInput = _wait.Until(d =>
            d.FindElement(By.Id("Name")));
        Assert.True(nameInput.Displayed);

        var descInput = _driver.FindElement(By.Id("Description"));
        Assert.True(descInput.Displayed);
    }
}
