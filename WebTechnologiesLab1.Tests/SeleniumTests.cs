using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace WebTechnologiesLab1.Tests;

// Базова URL сайту — змінити на актуальну перед запуском
// Наприклад: "https://localhost:7210" або "http://localhost:5000"
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

    // -------------------------------------------------------------------------
    // Допоміжний метод: гарантує, що тестовий користувач залогінений.
    // Якщо логін з фіксованими credentials падає — реєструє користувача
    // (RequireConfirmedAccount = false → автоматичний логін після Register).
    // -------------------------------------------------------------------------
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
            // Користувач ще не існує — реєструємо
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

    // -------------------------------------------------------------------------
    // Сценарій 1: Головна сторінка → клік "Перейти до Каталогу Товарів"
    //             → перевіряємо перехід на /Products і заголовок сторінки
    // -------------------------------------------------------------------------
    [Fact]
    public void HomePageCatalogButton_NavigatesToProductsPage()
    {
        // Arrange – логінимось і відкриваємо головну сторінку
        EnsureLoggedIn();
        _driver.Navigate().GoToUrl(BaseUrl);

        // Act – знаходимо кнопку "Перейти до Каталогу Товарів" і натискаємо
        var catalogButton = _wait.Until(d =>
            d.FindElement(By.CssSelector("a.btn-primary[href*='Products']")));
        catalogButton.Click();

        // Assert 1 – URL повинен містити /Products
        _wait.Until(d => d.Url.Contains("/Products"));
        Assert.Contains("/Products", _driver.Url);

        // Assert 2 – на сторінці є заголовок "Каталог Товарів"
        var heading = _wait.Until(d =>
            d.FindElement(By.CssSelector("h1")));
        Assert.Contains("Каталог Товарів", heading.Text);
    }

    // -------------------------------------------------------------------------
    // Сценарій 2: Форма логіну з невірними даними
    //             → вводимо email + пароль → перевіряємо повідомлення про помилку
    // -------------------------------------------------------------------------
    [Fact]
    public void LoginForm_InvalidCredentials_ShowsErrorMessage()
    {
        // Arrange – відкриваємо сторінку логіну
        _driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");

        // Act – заповнюємо поля email і password невірними даними
        var emailInput = _wait.Until(d =>
            d.FindElement(By.Id("Input_Email")));
        emailInput.Clear();
        emailInput.SendKeys("nonexistent@test.com");

        var passwordInput = _driver.FindElement(By.Id("Input_Password"));
        passwordInput.Clear();
        passwordInput.SendKeys("WrongPassword123!");

        // Натискаємо кнопку "Log in"
        var submitButton = _driver.FindElement(By.Id("login-submit"));
        submitButton.Click();

        // Assert 1 – залишаємось на сторінці логіну (URL містить Login)
        _wait.Until(d => d.Url.Contains("Login"));
        Assert.Contains("Login", _driver.Url);

        // Assert 2 – з'являється повідомлення про невірні дані (текст не пустий)
        var errorSummary = _wait.Until<IWebElement?>(d =>
        {
            var el = d.FindElement(By.CssSelector("div[role='alert']"));
            return !string.IsNullOrWhiteSpace(el.Text) ? el : null;
        });
        Assert.NotNull(errorSummary);
        Assert.Contains("Invalid", errorSummary!.Text);
    }

    // -------------------------------------------------------------------------
    // Сценарій 3: Сторінка Categories
    //             → перевіряємо таблицю (колонки Name, Description)
    //             → клікаємо "Create New" → перевіряємо форму створення категорії
    // -------------------------------------------------------------------------
    [Fact]
    public void CategoriesPage_TableVisible_AndCreateFormOpens()
    {
        // Arrange – відкриваємо список категорій
        _driver.Navigate().GoToUrl($"{BaseUrl}/Categories");

        // Assert 1 – таблиця відображається і є колонка "Name"
        var nameHeader = _wait.Until(d =>
            d.FindElement(By.XPath("//th[normalize-space()='Name']")));
        Assert.True(nameHeader.Displayed);

        // Assert 2 – є колонка "Description"
        var descHeader = _driver.FindElement(
            By.XPath("//th[normalize-space()='Description']"));
        Assert.True(descHeader.Displayed);

        // Act – клікаємо посилання "Create New"
        var createLink = _driver.FindElement(By.LinkText("Create New"));
        createLink.Click();

        // Assert 3 – URL містить /Categories/Create
        _wait.Until(d => d.Url.Contains("/Categories/Create"));
        Assert.Contains("/Categories/Create", _driver.Url);

        // Assert 4 – форма створення містить поле "Name"
        var nameInput = _wait.Until(d =>
            d.FindElement(By.Id("Name")));
        Assert.True(nameInput.Displayed);

        // Assert 5 – форма містить поле "Description"
        var descInput = _driver.FindElement(By.Id("Description"));
        Assert.True(descInput.Displayed);
    }
}
