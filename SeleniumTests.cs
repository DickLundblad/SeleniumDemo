using ClosedXML.Excel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumDemo.Models;

namespace SeleniumDemo
{
    public partial class SeleniumTests
    {
        private ChromeDriver driver; // Changed type from IWebDriver to ChromeDriver for improved performance
        private ChatGPTService? _chatService;

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
                    LoadEnvironmentVariables();
                    TestContext.WriteLine("Connected to existing browser");
                    TestContext.WriteLine("Current URL: " + driver.Url);

                    // Example: open a new tab and navigate
                    driver.Navigate().GoToUrl("https://example.com");
                    var chatGPTAPIKey = Environment.GetEnvironmentVariable("CHAT_GPT_API_KEY");
                    if (!string.IsNullOrEmpty(chatGPTAPIKey))
                    {
                        _chatService = new ChatGPTService(chatGPTAPIKey);
                    }
                    else
                    {
                        TestContext.WriteLine("CHATGPT_API_KEY is not set in the .env file.");
                    }
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
            }
        }

        [SetUp]
        public void SetUp()
        {
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]")]
        public void ValidateThatStartPageIsLoaded(string url, string selectorXPathForJobEntry, int delayUserInteraction = 0)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            driver.Navigate().GoToUrl(url);

            AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on page:{url}");

            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));

            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]", "")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]", "")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", "", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "")]
        public void ValidateThatJoblinksCanBeRetrievedFromStartPages(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction = 0)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            driver.Navigate().GoToUrl(url);

            AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on page: {url}");

            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));

            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
            List<JobListing> jobListings = new();
            foreach (var node in jobNodes)
            {
                var jobListing = new JobListing
                {
                    JobLink = SeleniumTestsHelpers.ExtractHref(addDomainToJobPaths, node) ?? string.Empty
                };
                jobListings.Add(jobListing);
                Assert.That(jobListing.JobLink, Is.Not.Empty, "Job link is empty");
            }
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "jobbsafari_se_data_och_it_newest", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]", "se_indeed_empty_what_where", "")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","se_jooble_org", "", 2000)]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", "monster_se_mjukvara_skane", "", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "linkedin_com_it-services-and-it-consulting", "")]
        [TestCase("https://www.linkedin.com/jobs/search/?currentJobId=4205944474&geoId=105117694&keywords=software&origin=JOB_SEARCH_PAGE_SEARCH_BUTTON&refresh=true", "//div[@data-job-id]", "linkedin_com_software", "")]
        public void ValidateThatJoblinksCanBeRetrievedAndParsed(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            driver.Navigate().GoToUrl(url);

            AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));
            if (jobNodes.Count == 0)
            {
                Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on start page {url}");
            }
            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
            List<JobListing> jobListings = new();
            foreach (var node in jobNodes)
            {
                var jobListing = new JobListing();
                jobListing.JobLink = SeleniumTestsHelpers.ExtractHref(addDomainToJobPaths, node);
                jobListings.Add(jobListing);
            }
            //loop over each jobListing, open link and extract info
            foreach (var jobListing in jobListings)
            {
                Thread.Sleep(delayUserInteraction);
                var updatedJobListing = OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
            }

           // var tsvFilePath = SeleniumTestsHelpers.GenerateFileNameForUrl(url);
            SeleniumTestsHelpers.WriteListOfJobsToFile(jobListings, fileName, "JobListings");
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/jobb/digital-radio-system-designer-sesri-19207406", 0)]
        [TestCase("https://www.linkedin.com/jobs/view/4194781616/?eBP=BUDGET_EXHAUSTED_JOB&refId=wqmOM1Whbos%2BqR2hax6d%2BQ%3D%3D&trackingId=Y31jWZzmfvJYm7mUln7UBQ%3D%3D&trk=flagship3_job_collections_leaf_page", 0)]
        [TestCase("https://se.jooble.org/desc/-154934751721925931?ckey=NONE&rgn=-1&pos=1&elckey=3819297206643930044&pageType=20&p=1&jobAge=2608&relb=140&brelb=100&bscr=112&scr=156.8&premImp=1", 0)]
        [TestCase("https://se.jooble.org/desc/-2750184788513872086?ckey=NONE&rgn=-1&pos=3&elckey=3819297206643930044&pageType=20&p=1&jobAge=766&relb=100&brelb=100&bscr=88.1224&scr=88.1224", 2000)]
        [TestCase("https://jobbsafari.se/jobb/solution-architect-intralogistics-development-supply-chain-development-siske-19207507", 0)]
        [TestCase("https://jobbsafari.se/jobb/rd-specialist-till-essentias-protein-solutions-sesmp-19206771", 0)]
        [TestCase("https://www.monster.se/jobberbjudande/it-tekniker-till-internationellt-f%C3%B6retag-malm%C3%B6-sk%C3%A5ne--24828633-9781-4533-95c8-6dc9c2758f21?sid=755339e0-795d-402e-b468-2e6ca4790ae9&jvo=m.mp.s-svr.1&so=m.s.lh&hidesmr=1", 2000)]
        public void ValidateThatAJobLinkCanBeOpenedAndParsed(string url, int delayUserInteraction = 0)
        {
            var jobListing = OpenAndParseJobLink(url, delayUserInteraction);
            // JobLink can change if it's a re-direct, but we will keep the original URL
            Assert.That(jobListing.JobLink, Is.EqualTo(url), "Job link is not url");
        }

        [Category("live")]
        [TestCase("JobListingsExcel_ClosedXml_.xlsx", "*.tsv", "JobListings")]
        public void ZZ_CreateExcelSheetWithJobListingsUsingClosedXML(string fileName, string filePattern, string subFolder ="")
        {
            var files = GetFileNames(filePattern, subFolder);
            if (files != null)
            {
                SeleniumTestsHelpers.CreateExcelFromExistingFiles(fileName, files);
            }
            else
            {
                TestContext.WriteLine($"No files found with pattern: {filePattern}");
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
        private string[]? GetFileNames(string searchPatternForFiles, string subPath)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), subPath);
            var files = Directory.GetFiles(folder, searchPatternForFiles);
            if (files.Length == 0)
            {
                TestContext.WriteLine("No files found.");
                return null;
            }
            return files;
        }
        private JobListing OpenAndParseJobLink(string url, int delayUserInteraction)
        {
            var jobListing = new JobListing();
            jobListing.JobLink = url;
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
                driver.SwitchTo().Window(driver.WindowHandles.Last());
                driver.Navigate().GoToUrl(url);
                AcceptPopups();
                Thread.Sleep(delayUserInteraction);
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
                try
                {
                    WaitForDocumentReady(wait);
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Error during WaitForDocumentReady() : {ex.Message}");
                }
                Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on jobLink page: {url}");
                if (url.Contains("linkedin"))
                { 
                    ShowMore();
                }
                // extract info on page
                jobListing.Title = ExtractTitle();
                jobListing.ContactInformation = ExtractContactInfo();
                jobListing.Published = ExtractPublishedDate();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Warning Exception OpenAndParseJobLink({url}) , exception message: {ex.Message}");
            }
            return jobListing;
        }

        private static void WaitForDocumentReady(WebDriverWait wait)
        {
            bool IsDocumentReady = wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        }

        private bool BlockedInfoOnPage()
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            TestContext.WriteLine($"BlockedInfoOnPage():");
            WaitForDocumentReady(wait);
            string[] xPaths =
            [
                "//*[contains(text(), 'blockerad')]",
                    "//*[contains(text(), 'blocked')]",
                    "//*[contains(@title, 'captcha')]",
                    "//*[contains(normalize-space(), 'Bekräfta att du är en människa')]",
                    "//*[contains(normalize-space(), 'Verify you are human')]",
                    "//*[contains(text, 'Vi vill försäkra oss om att vi vänder oss till dig och inte till en robot')]",
                    "//*[contains(normalize-space(), 'Additional Verification Required')]",
                    "//*[contains(text(), 'captcha')]",
                    "//*[contains(@id, 'captcha')]",
                    "//*[contains(@class, 'captcha')]"
            ];

            foreach (var xPath in xPaths)
            {
                var elements = driver.FindElements(By.XPath(xPath));
                if (elements.Count > 0)
                {
                    foreach (var element in elements)
                    {
                        if (element.Displayed)
                        {
                            TestContext.WriteLine($"Element is displayed: {element.Text}");
                            TestContext.WriteLine($"Element found and Displayed, element.TagName: {element.TagName}");
                            var bodyText = GetElementTextOnCurrentPage("//body");
                            TestContext.WriteLine("Body Text:");
                            TestContext.WriteLine(bodyText);
                            return true;
                        }
                        else
                        {
                            TestContext.WriteLine($"Blocked element exists for xPath: {xPath}, but it is not displayed: {element.Text}. xPath: {xPath}");
                        }
                    }
                }
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

        private void ShowMore()
        {
            try
            {
                // Wait for the button to be clickable (recommended)
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            
                // Option 1: Using aria-label (most reliable)

                    // Replace the usage of ExpectedConditions with WebDriverWait's lambda-based approach
                    try
                    {
                        // Wait for the button to be clickable using a lambda expression
                        IWebElement seeMoreButton = wait.Until(driver =>
                            driver.FindElement(By.CssSelector("button[aria-label='Click to see more description']")));

                        // Scroll into view if needed
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", seeMoreButton);

                        // Click the button
                        seeMoreButton.Click();
                        Console.WriteLine($"Clicked see more button");
                    }
                    catch (NoSuchElementException ex)
                    {
                        Console.WriteLine($"Button not found: {ex.Message}");
                    }
                    catch (ElementClickInterceptedException ex)
                    {
                        Console.WriteLine($"Could not click button: {ex.Message}");
                        // You might need to handle overlays or add more wait time here
                    }

            
                // Alternative: Option 2 using XPath
                /*
                IWebElement seeMoreButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("//button[.//span[contains(text(), 'See more')]]"));
                seeMoreButton.Click();
                */
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine($"Button not found: {ex.Message}");
            }
            catch (ElementClickInterceptedException ex)
            {
                Console.WriteLine($"Could not click button: {ex.Message}");
                // You might need to handle overlays or add more wait time here
            }
        }

        private string GetElementTextOnCurrentPage(string xPath)
        {
            try
            {
                var bodyElement = driver.FindElement(By.XPath(xPath));
                return bodyElement.Text;
            }
            catch (NoSuchElementException ex)
            {
                TestContext.WriteLine($"Error: {ex.Message}");
                return string.Empty;
            }
        }

        private void LoadEnvironmentVariables()
        {
            var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envFilePath))
            {
                var lines = File.ReadAllLines(envFilePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"));

                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                    }
                }
            }
            else
            {
                TestContext.WriteLine(".env file not found.");
            }
        }
    }
}