using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumDemo.Models;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using OpenQA.Selenium.Support.UI;

namespace SeleniumDemo
{
    public class SeleniumTests
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
        public void ValidateThatJoblinksCanBeRetrievedFromPages(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction=0) 
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
                jobListing.JobLink = ExtractHref(addDomainToJobPaths, node);
                jobListings.Add(jobListing);
                Assert.That(jobListing.JobLink, Is.Not.Empty, "Job link is empty"); 
            }
        }

        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]","")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","")]
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
                jobListing.JobLink = ExtractHref(addDomainToJobPaths, node);
                jobListings.Add(jobListing);
             }
            foreach (var jobListing in jobListings) 
            {       
                var updatedJobListing = OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
              }

            var fileName = RemoveInvalidChars(ReplaceBadCharactersInFilePath(url));
            var tsvFilePath = $"JobListings_{fileName}.tsv";
            WriteToFile(jobListings, tsvFilePath);
        }

        [TestCase("https://jobbsafari.se/jobb/digital-radio-system-designer-sesri-19207406", 0)]
        [TestCase("https://www.linkedin.com/jobs/view/4194781616/?eBP=BUDGET_EXHAUSTED_JOB&refId=wqmOM1Whbos%2BqR2hax6d%2BQ%3D%3D&trackingId=Y31jWZzmfvJYm7mUln7UBQ%3D%3D&trk=flagship3_job_collections_leaf_page", 0)]
        [TestCase("https://se.jooble.org/desc/-154934751721925931?ckey=NONE&rgn=-1&pos=1&elckey=3819297206643930044&pageType=20&p=1&jobAge=2608&relb=140&brelb=100&bscr=112&scr=156.8&premImp=1", 0)]
        [TestCase("https://jobbsafari.se/jobb/solution-architect-intralogistics-development-supply-chain-development-siske-19207507", 0)]
        [TestCase("https://jobbsafari.se/jobb/rd-specialist-till-essentias-protein-solutions-sesmp-19206771", 0)]
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
            try {
                ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
                driver.SwitchTo().Window(driver.WindowHandles.Last());
                driver.Navigate().GoToUrl(url);
                AcceptPopups();
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                Thread.Sleep(delayUserInteraction);
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

     private  string ExtractPublishedDate() 
     {
        string response = string.Empty;
        
        TestContext.WriteLine($"Extracted.PublishedDate: {response}");
      
        return response;
     }

     public string ExtractTitle()
     {
        string response = string.Empty;
        var titleNode = driver.FindElement(By.XPath("//h1"));
        response = titleNode.Text;
        TestContext.WriteLine($"Extracted.Title: {response}");
      
        return response;
    }
    private string ExtractContactInfo()
    {
        string response = string.Empty;
        var bodyNode = driver.FindElement(By.XPath("//body"));
        
        if (_chatService != null)
        {
            string prompt = $@"extract contact information and roles from this text in the same language as the text: {bodyNode.Text}";
            var task =_chatService.GetChatResponse(prompt);
            if (task != null) 
            {
                    response = task.Result;
            }
            if (response != string.Empty) 
            {
                return response;
            }else 
            {
                TestContext.WriteLine($"ChatGPT returned empty response for prompt: {prompt}");
            }
         }    
        response = ExtractPhoneNumbersFromAreaCodeExtractions(bodyNode.Text);

        if (string.IsNullOrEmpty(response))
        {
            response = ExtactContactInfoFromHtml(bodyNode.Text);
        }
        TestContext.WriteLine($"Extracted ContactInfo: {response}");
        return response;
    }

    public string ExtactContactInfoFromHtml(string html)
    {
        var results = new List<string>();
        string pattern = @"(.{0,125}?)(\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b|07\d{2}-\d{6})(.{0,50}?)";

        foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase))
        {
            if (match.Groups.Count >= 4)
            {
                string before = match.Groups[1].Value.Trim();
                string contact = match.Groups[2].Value.Trim();
                string after = match.Groups[3].Value.Trim();

                results.Add($"{before} {contact} {after}".Trim());
            }
        }

        return string.Join(", ", results);
    }
    public string ExtractPhoneNumbersFromAreaCodeExtractions(string html, string countryCode = "+46")
    {
        // Regex for phone numbers starting with +46, allowing spaces inside the number
        var phoneRegex = new Regex(@$"\{countryCode}[\s\-]?[0-9\s\-]+");
        var matches = phoneRegex.Matches(html);

        var result = new List<string>();

        foreach (Match match in matches)
        {
            // Find the name associated with the phone number
            var phoneIndex = html.IndexOf(match.Value);
            var nameStartIndex = html.LastIndexOfAny(new char[] { '.', ';' }, phoneIndex) + 1;
            var nameEndIndex = phoneIndex;
            var name = html.Substring(nameStartIndex, nameEndIndex - nameStartIndex).Trim();

            result.Add($"{name}, {match.Value}");
        }

        return string.Join(", ", result);
    }

        [TestCase("JobListingsExcel_ClosedXml_.xlsx", "*Joblistings*.tsv")]
        public void ZZ_CreateExcelSheetWithJobListingsUsingClosedXML(string fileName, string filePattern) 
         {
            WriteToExcelSheetUsingClosedXML(fileName,filePattern);
         }

        /// <summary>
        /// Grabas existing TSV files and writes them to an Excel sheet using ClosedXML.
        /// </summary>
        /// <param name="fileName">Filename of excelsheet</param>
        /// <param name="searchPatternForFiles"></param>
        private void WriteToExcelSheetUsingClosedXML(string fileName, string searchPatternForFiles) 
        {
            var tsvFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPatternForFiles);
            if (tsvFiles.Length == 0) 
            {
                TestContext.WriteLine("No TSV files found.");
                return;
            }
            using (var workbook = new XLWorkbook())
            {
                foreach (var tsvFile in tsvFiles)
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
        private static void WriteToFile(List<JobListing> results, string tsvFilePath) 
        {
            // Log the job listings
            foreach (var jobListing in results) 
            {
                TestContext.WriteLine($"JobLink: {jobListing.JobLink}, Title: {jobListing.Title}, Description: {jobListing.Description}");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
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
                    if (!string.IsNullOrEmpty(jobListing.JobLink)) // Ensure JobLink is not empty
                    {
                        csv.WriteRecord(jobListing);
                        csv.NextRecord();
                    }else 
                    {
                        TestContext.WriteLine($"JobLink is empty for job listing: {jobListing.Title}");
                    }
                }
             }

            TestContext.WriteLine($"TSV file created: {tsvFilePath}");
            using (var reader = new StreamReader(tsvFilePath))
            using (var csvR = new CsvReader(reader, config)) 
            {
                    var records = csvR.GetRecords<JobListing>().ToList();
                    Assert.That(records.Count, Is.GreaterThan(0), "The TSV file does not contain any job listings.");
                    TestContext.WriteLine($"Validated that the TSV file contains {records.Count} job listings.");
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

private static string ExtractHref(string addDomainToJobPaths, IWebElement jobNode) {
            string jobLink = string.Empty;
            try {
                jobLink = RetryFindAttribute(jobNode, "href");
                if (!string.IsNullOrEmpty(jobLink)) {
                    jobLink = addDomainToJobPaths + jobLink;
                    }
                if (string.IsNullOrEmpty(jobLink)) {
                    var anchorTag = jobNode.FindElement(By.TagName("a"));
                    jobLink = RetryFindAttribute(anchorTag, "href");
                    }
                if (string.IsNullOrEmpty(jobLink)) {
                    var innerHtml = RetryFindAttribute(jobNode, "innerHTML");
                    }
                TestContext.WriteLine($"Job link: {jobLink}");
                } catch (Exception ex) {
                TestContext.WriteLine($"Could not GetAttribute(\"href\") for {ex.InnerException}");
                throw;
                }
            return jobLink;
            }

        private static string RetryFindAttribute(IWebElement element, string attribute, int retryCount = 3) {
            while (retryCount-- > 0) {
                try {
                    return element.GetAttribute(attribute);
                    } catch (StaleElementReferenceException) {
                      Thread.Sleep(1000); // Wait for 1 second before retrying
                    }
                }
            throw new StaleElementReferenceException($"Element is stale after {retryCount} retries");
            }

        private bool BlockedInfoOnPage() 
        {
   
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
            /*var allHtml = driver.PageSource;
            TestContext.WriteLine("HTML after ensuring page load:");
            TestContext.WriteLine(allHtml);

            var temp = driver.PageSource;
            TestContext.WriteLine("Raw HTML:");
            TestContext.WriteLine(allHtml);
            
            var decodedText = System.Net.WebUtility.HtmlDecode(driver.PageSource);
            TestContext.WriteLine("Decoded allHtml Text:");
            TestContext.WriteLine(decodedText);

            var bodyText = GetElementTextOnCurrentPage("//body");
            TestContext.WriteLine("Body Text:");
            TestContext.WriteLine(bodyText);*/
            
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
                            TestContext.WriteLine($"Blocked element exists for xPath{xPath}, but is is not displayed: {element.Text}");
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
    }
}