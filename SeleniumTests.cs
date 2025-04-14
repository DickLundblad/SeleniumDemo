using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumDemo.Models;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using OpenQA.Selenium.Support.UI;
using System.Xml.Linq;

namespace SeleniumDemo
{
    public partial class SeleniumTests
    {
        private ChromeDriver driver; // Changed type from IWebDriver to ChromeDriver for improved performance
        private ChatGPTService _chatService;

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
                    // read from .env file instead
                    var chatGPTAPIKey = "";
                    if(! string.IsNullOrEmpty(chatGPTAPIKey))
                    { 
                        _chatService = new ChatGPTService(chatGPTAPIKey);
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
            Assert.That(BlockedInfoOnPage(),Is.False,$"Blocked on page:{url}");

            var jobNodes = driver.FindElements(By.XPath(selectorXPathForJobEntry));
  
            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }

        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]","")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']","", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "")]
        public void ValidateThatJoblinksCanBeRetrievedFromStartPages(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction=0) 
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
            List<JobListing> jobListings = [];
            foreach (var node in jobNodes)
            {
                var jobListing = new JobListing();
                jobListing.JobLink = SeleniumTestsHelpers.ExtractHref(addDomainToJobPaths, node);
                jobListings.Add(jobListing);
                Assert.That(jobListing.JobLink, Is.Not.Empty, "Job link is empty"); 
            }
        }

        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]","")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","", 2000)]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']","", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "")]
        public void ValidateThatJoblinksCanBeRetrievedAndParsed(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction=0) 
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
            List<JobListing> jobListings = [];
            foreach (var node in jobNodes)
            {
                var jobListing = new JobListing();
                jobListing.JobLink = SeleniumTestsHelpers.ExtractHref(addDomainToJobPaths, node);
                jobListings.Add(jobListing);
             }
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

            //var fileName = SeleniumTestsHelpers.RemoveInvalidChars(SeleniumTestsHelpers.ReplaceBadCharactersInFilePath(url));
            //var tsvFilePath = $"JobListings_{fileName}.tsv";
            var tsvFilePath =  SeleniumTestsHelpers.GenerateTsvFileNameForUrl(url);
            SeleniumTestsHelpers.WriteListOfJobsToFile(jobListings, tsvFilePath);
        }

        [TestCase("https://jobbsafari.se/jobb/digital-radio-system-designer-sesri-19207406", 0)]
        [TestCase("https://www.linkedin.com/jobs/view/4194781616/?eBP=BUDGET_EXHAUSTED_JOB&refId=wqmOM1Whbos%2BqR2hax6d%2BQ%3D%3D&trackingId=Y31jWZzmfvJYm7mUln7UBQ%3D%3D&trk=flagship3_job_collections_leaf_page", 0)]
        [TestCase("https://se.jooble.org/desc/-154934751721925931?ckey=NONE&rgn=-1&pos=1&elckey=3819297206643930044&pageType=20&p=1&jobAge=2608&relb=140&brelb=100&bscr=112&scr=156.8&premImp=1", 0)]
        [TestCase("https://jobbsafari.se/jobb/solution-architect-intralogistics-development-supply-chain-development-siske-19207507", 0)]
        [TestCase("https://jobbsafari.se/jobb/rd-specialist-till-essentias-protein-solutions-sesmp-19206771", 0)]
        [TestCase("https://www.monster.se/jobberbjudande/it-tekniker-till-internationellt-f%C3%B6retag-malm%C3%B6-sk%C3%A5ne--24828633-9781-4533-95c8-6dc9c2758f21?sid=755339e0-795d-402e-b468-2e6ca4790ae9&jvo=m.mp.s-svr.1&so=m.s.lh&hidesmr=1", 2000)]
        public void ValidateThatAJobLinkCanBeOpenedAndParsed(string url, int delayUserInteraction=0) 
        {
            var jobListing = OpenAndParseJobLink(url, delayUserInteraction);
            // JobLink can change if it's a re-direct, but we will keep the original URL
            Assert.That(jobListing.JobLink, Is.EqualTo(url), "Job link is not url");
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
                } catch (Exception ex) 
                {
                    TestContext.WriteLine($"Error during WaitForDocumentReady() : {ex.Message}");
                }
                Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on jobLink page: {url}");
                // extract info on page
                jobListing.Title = ExtractTitle();
                jobListing.ContactInformation = ExtractContactInfo();
                jobListing.Published = ExtractPublishedDate();
            } catch (Exception ex)
            {
                TestContext.WriteLine($"Warning Exception OpenAndParseJobLink({url}) , exception message: {ex.Message}");
            }
            return jobListing;
       }

        private static void WaitForDocumentReady(WebDriverWait wait) 
        {
            bool IsDocumentReady = wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        }

     private  string ExtractPublishedDate() 
     {
        string response = string.Empty;
        
        TestContext.WriteLine($"Extracted.PublishedDate: {response}");
      
        return response;
     }





        [TestCase("JobListingsExcel_ClosedXml_.xlsx", "*Joblistings*.tsv")]
        public void ZZ_CreateExcelSheetWithJobListingsUsingClosedXML(string fileName, string filePattern) 
         {
            var files = GetFileNames(filePattern);
            if (files != null)
            { 
                WriteToExcelSheetUsingClosedXML(fileName,files);
            }else
            {
                TestContext.WriteLine($"No files found with pattern: {filePattern}");
            }
         }

        private string[]? GetFileNames(string searchPatternForFiles) 
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPatternForFiles);
            if (files.Length == 0) 
            {
                TestContext.WriteLine("No TSV files found.");
                return null;
            }
            return files;
        }
        /// <summary>
        /// Grabas existing TSV files and writes them to an Excel sheet using ClosedXML.
        /// </summary>
        /// <param name="fileName">Filename of excelsheet</param>
        /// <param name="searchPatternForFiles"></param>
        private void WriteToExcelSheetUsingClosedXML(string fileName, string[] files) 
        {
            using (var workbook = new XLWorkbook())
            {
                foreach (var tsvFile in files)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(tsvFile);
                    fileNameWithoutExtension = fileNameWithoutExtension.Substring(0,30);
                    var worksheet = workbook.Worksheets.Add(fileNameWithoutExtension);
                    int row = 1;

                    foreach (var line in File.ReadLines(tsvFile))
                    {
                        var columns = line.Split('\t');
                        for (int col = 0; col < columns.Length; col++)
                        {
                            worksheet.Cell(row, col + 1).Value = columns[col];
                        }
                        row++;
                     }
                }
                workbook.SaveAs(fileName);
            }
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
                //driver.FindElements(By.XPath("//*[contains(text(), 'blocked')]"));
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
                        }else 
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

        [OneTimeTearDown]
        public void TearDown()
        {
            driver.Quit();
            driver.Dispose();
        }
        private string GetElementTextOnCurrentPage(string xPath) {
            try {
                var bodyElement = driver.FindElement(By.XPath(xPath));
                return bodyElement.Text;
                } catch (NoSuchElementException ex) {
                TestContext.WriteLine($"Error: {ex.Message}");
                return string.Empty;
                }
            }
        private string GetAllHtmlOnCurrentPage() {
            try {
                return driver.PageSource;
                } catch (Exception ex) {
                TestContext.WriteLine($"Error: {ex.Message}");
                return string.Empty;
                }
            }
        private void RefreshPage() {
            driver.Navigate().Refresh();
            TestContext.WriteLine("Page has been refreshed.");
            }
    }
}