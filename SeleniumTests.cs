using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

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

        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']",  2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]")]
        public void ValidateThatStartPageIsLoaded(string url, string selectorXPathForJobEntry, int delayUserInteraction=0)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            driver.Navigate().GoToUrl(url);

            AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            Assert.That(BlockedInfoOnPage(),Is.False,"Blocked on page");

            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));
  
            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }

        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]","")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']","", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "")]
        public void ValidateThatJoblinksCanBeRetrievedFromPages(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction=0) 
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            driver.Navigate().GoToUrl(url);

            AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            Assert.That(BlockedInfoOnPage(), Is.False, "Blocked on page");

            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));

            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            ExtractHrefs(addDomainToJobPaths, jobNodes);
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }

        private static void ExtractHrefs(string addDomainToJobPaths, System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> jobNodes) 
        {
            foreach (var jobNode in jobNodes) ExtractHref(addDomainToJobPaths, jobNode);
        }

        private static void ExtractHref(string addDomainToJobPaths, IWebElement jobNode) 
        {
            var jobLink = jobNode.GetAttribute("href");
            if (string.IsNullOrEmpty(jobLink)) {
                var anchorTag = jobNode.FindElement(By.TagName("a"));
                jobLink = anchorTag?.GetAttribute("href");
                }
            if (string.IsNullOrEmpty(jobLink)) {
                var innerHtml = jobNode.GetAttribute("innerHTML");
                TestContext.WriteLine($"innerHTML: {innerHtml}");
                }
            if (!string.IsNullOrEmpty(addDomainToJobPaths)) {
                jobLink = addDomainToJobPaths + jobLink;
                }
            TestContext.WriteLine($"Job link: {jobLink}");
        }

        private bool BlockedInfoOnPage() 
        {
            if (driver.FindElements(By.XPath("//*[contains(text(), 'blockerad')]")).Count > 0) 
            {
                string[] xPaths = new string[]
                {
                    "//*[contains(text(), 'blockerad')]",
                    "//*[contains(text(), 'captcha')]",
                    "//*[contains(@id, 'captcha')]",
                    "//*[contains(@class, 'captcha')]"
                };

                foreach (var xPath in xPaths) 
                {
                    if (driver.FindElements(By.XPath(xPath)).Count > 0) 
                    {
                        TestContext.WriteLine($"BlockedInfoOnPage(): {xPath}");
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        private void AcceptPopups()
        {
            try
            {
                var acceptCookiesButton = driver.FindElement(By.XPath("//button[contains(text(), 'Accept Cookies')]"));
                var accepteraButton = driver.FindElement(By.XPath("//button[contains(text(), 'Acceptera')]"));
                var approveButton = driver.FindElement(By.XPath("//button[contains(text(), 'Godkänn')]"));

                if (acceptCookiesButton.Displayed)
                {
                    acceptCookiesButton.Click();
                }
                if (accepteraButton.Displayed)
                {
                    acceptCookiesButton.Click();
                }
                if (approveButton.Displayed)
                {
                    approveButton.Click();
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