using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumDemo.Models;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;

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
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
            List<JobListing> jobListings = [];
            foreach (var node in jobNodes)
            {
                var jobListing = new JobListing();
                jobListing.JobLink = ExtractHref(addDomainToJobPaths, node);
                jobListings.Add(jobListing);
             }

            var fileName = RemoveInvalidChars(ReplaceBadCharactersInFilePath(url));
            var tsvFilePath = $"JobListings_{fileName}.tsv";
            WriteToFile(jobListings, tsvFilePath);
        }

        // ...

        [TestCase("JobListingsExcel", "*Joblistings*.tsv")]        public void Z_CreateExcelSheetWithJobListings(string fileName, string filePattern) {
            var tsvFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), filePattern);
            if (tsvFiles.Length == 0) {
                TestContext.WriteLine("No TSV files found.");
                return;
                }
            var excelFilePath = $"{fileName}.xlsx";
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
            using (var package = new ExcelPackage()) {
                foreach (var tsvFile in tsvFiles) {
                    var worksheet = package.Workbook.Worksheets.Add(Path.GetFileNameWithoutExtension(tsvFile));
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                        Delimiter = "\t"
                        };

                    using (var reader = new StreamReader(tsvFile))
                    using (var csv = new CsvReader(reader, config)) {
                        var records = csv.GetRecords<dynamic>().ToList();
                        if (records.Count == 0) {
                            TestContext.WriteLine($"No records found in file: {tsvFile}");
                            continue;
                            }

                        worksheet.Cells["A1"].LoadFromCollection(records, true);
                        TestContext.WriteLine($"Loaded {records.Count} records from file: {tsvFile}");
                        }
                    }
                package.SaveAs(new FileInfo(excelFilePath));
                TestContext.WriteLine($"Excel file created: {excelFilePath}");
                }

            // Validate that each tab has some rows with data
            using (var package = new ExcelPackage(new FileInfo(excelFilePath))) {
                foreach (var worksheet in package.Workbook.Worksheets) {
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    Assert.That(rowCount, Is.GreaterThan(1), $"Worksheet {worksheet.Name} has no data rows.");
                    TestContext.WriteLine($"Worksheet {worksheet.Name} has {rowCount - 1} data rows.");

                    // Validate that the column JobLink has value for each row
                    for (int row = 2; row <= rowCount; row++) {
                        var jobLink = worksheet.Cells[row, 1].Text; // Assuming JobLink is in the first column
                        Assert.That(string.IsNullOrEmpty(jobLink), Is.False, $"Row {row} in worksheet {worksheet.Name} has an empty JobLink.");
                        }

                    // Validate that each sheet has a header row
                    var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns];
                    Assert.That(headerRow, Is.Not.Null, $"Worksheet {worksheet.Name} does not have a header row.");
                    }
                }
            }

        private static void WriteToFile(List<JobListing> results, string tsvFilePath)
        {
            // Remove the line causing the error
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t"
            };

            using (var writer = new StreamWriter(tsvFilePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<JobListing>();
                csv.NextRecord();
                foreach (var jobListing in results)
                {
                    // Remove invalid characters
                    jobListing.Title = RemoveInvalidChars(jobListing.Title);
                    jobListing.Description = RemoveInvalidChars(jobListing.Description);

                    csv.WriteRecord(jobListing);
                    csv.NextRecord();
                }
            }
        }
        public static string ReplaceBadCharactersInFilePath(string input)
        {
            return input.Replace(":", "_")
                .Replace("//", "_").
                Replace("/", "_").
                Replace(".", "_").
                Replace("?", "_").
                Replace("=", "_").
                Replace("%", "_").
                Replace(")", "_").
                Replace("(", "_");
        }
        public static string RemoveInvalidChars(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Hardcoded invalid characters for Windows
            char[] invalidCharsForWindowsAndLinux = { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' };

            Console.WriteLine($"Invalid chars: {string.Join(", ", invalidCharsForWindowsAndLinux)}");
            return new string(input.Where(ch => !invalidCharsForWindowsAndLinux.Contains(ch)).ToArray());
        }
        private static string ExtractHref(string addDomainToJobPaths, IWebElement jobNode) 
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
            return jobLink;
            }

        private bool BlockedInfoOnPage() 
        {
            if (driver.FindElements(By.XPath("//*[contains(text(), 'blockerad')]")).Count > 0) 
            {
                string[] xPaths = new string[]
                {
                    "//*[contains(text(), 'blockerad')]",
                    "*[contains(normalize-space(), 'Bekräfta att du är en människa')]",
                    "*[contains(normalize-space(), 'Additional Verification Required')]",
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