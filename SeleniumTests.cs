using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Remote;
using System;

namespace SeleniumDemo
{
    public class SeleniumTests
    {
        private ChromeDriver driver; // Changed type from IWebDriver to ChromeDriver for improved performance

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            try
            {

                // Check if any Chrome instances are already running
                var chromeProcesses = System.Diagnostics.Process.GetProcessesByName("chrome");
                if (chromeProcesses.Length == 0)
                {
                    TestContext.WriteLine("No Chrome instances found, start a new one.");
                    System.Diagnostics.Process.Start(@"C:\Program Files\Google\Chrome\Application\chrome.exe",
                        @"--remote-debugging-port=9222 --user-data-dir=C:\ChromeDebug");

                    // Optional: wait a bit for Chrome to fully initialize
                    Thread.Sleep(2000);
                }
                else
                {
                    TestContext.WriteLine("An existing Chrome instance is already running.");
                }

                var options = new ChromeOptions();
                options.DebuggerAddress = "127.0.0.1:9222"; // Connect to the debugging port

                // Attempt to connect to the existing Chrome instance
                driver = new ChromeDriver(options);

                try
                {
                    TestContext.WriteLine("Connected to existing browser");
                    TestContext.WriteLine("Current URL: " + driver.Url);

                    // Example: open a new tab and navigate
                    driver.Navigate().GoToUrl("https://example.com");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            catch (WebDriverException ex)
            {
                TestContext.WriteLine($"Failed to connect to the existing Chrome instance: {ex.Message}");
                TestContext.WriteLine("Falling back to launching a new ChromeDriver instance...");
                Assert.Fail($"Failed to connect to the existing Chrome instance.s.{ex.Message}");

                // Fallback to opening a new ChromeDriver instance if RemoteWebDriver fails
                driver = new ChromeDriver();
            }
        }

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void ValidateThatTheMessageIsDisplayed()
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.Navigate().GoToUrl("https://www.lambdatest.com/selenium-playground/simple-form-demo");
            driver.FindElement(By.Id("user-message")).SendKeys("LambdaTest rules");
            driver.FindElement(By.Id("showInput")).Click();
            Assert.IsTrue(driver.FindElement(By.Id("message")).Text.Equals("LambdaTest rules"),
                          "The expected message was not displayed.");
        }

        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]","")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "/*[@data-testid='jobTitle']","", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "")]
        public void ValidateThatPageIsLoaded(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction=0)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            driver.Navigate().GoToUrl(url);

            AcceptCookiesIfPresent();
            Thread.Sleep(delayUserInteraction);
            try
            {
                var accepteraButton = driver.FindElement(By.XPath("//button[contains(text(), 'Acceptera')]"));
                if (accepteraButton.Displayed)
                {
                    accepteraButton.Click();
                }
            }
            catch (NoSuchElementException)
            {
            }

            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));
            if (Blocked())
            {
                Assert.Fail("Blocked on page");
            }    
            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            Console.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }
        private bool Blocked() {
            try {
                var blockedElement = driver.FindElement(By.XPath("//div[@class='blocked']"));
                return blockedElement.Displayed;
                } catch (NoSuchElementException) {
                // Element not found, not blocked
                if (driver.FindElements(By.XPath("//*[contains(text(), 'Blockerad')]")).Count > 0) {
                    return true;
                    }
                if (driver.FindElements(By.XPath("//div[@class='blocked']")).Count > 0) {
                    return true;
                    }
                } catch (ElementClickInterceptedException) {
                return false;
                } catch (StaleElementReferenceException) {
                // Element is no longer attached to the DOM
                return false;
                } catch (ElementNotInteractableException) {
                }
            return true;
            }
                    // Element is not interactable
        private void AcceptCookiesIfPresent()
        {
            try
            {
                var acceptCookiesButton = driver.FindElement(By.XPath("//button[contains(text(), 'Accept Cookies')]"));
                var accepteraButton = driver.FindElement(By.XPath("//button[contains(text(), 'Acceptera')]"));
                if (acceptCookiesButton.Displayed)
                {
                    acceptCookiesButton.Click();
                }
                if (accepteraButton.Displayed)
                {
                    acceptCookiesButton.Click();
                }
            }
            catch (NoSuchElementException)
            {
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}