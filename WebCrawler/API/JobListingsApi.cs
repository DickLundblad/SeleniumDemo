using DocumentFormat.OpenXml.Bibliography;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebCrawler.Models;
using WebCrawler;
using System.Collections.Generic;
using System.Threading;

public class JobListingsApi
{
    private  IWebDriver _driver;
    private ChatGPTService? _chatService;

    public IWebDriver driver => _driver;

    public JobListingsApi(IWebDriver driver)
    {
        _driver = driver;
        LoadEnvironmentVariables();
        var chatGPTAPIKey = Environment.GetEnvironmentVariable("CHAT_GPT_API_KEY");
        if (!string.IsNullOrEmpty(chatGPTAPIKey))
        {
            _chatService = new ChatGPTService(chatGPTAPIKey);
        }
        else
        {
            Console.WriteLine("CHATGPT_API_KEY is not set in the .env file.");
        }
    }

    public void CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true)
    {
        List<JobListing> liveJobListings = OpenAndExtractJobListings(url, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction, removeParams);
        var savedJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, "JobListings");
        var newJobListings = SeleniumTestsHelpers.ExtractNewJobListings(liveJobListings, savedJobListings.JobListingsList);

        if (newJobListings.Count >0)
        {
            Console.WriteLine($"Number of new job listings to open and parse: {newJobListings.Count}");
            foreach (var jobListing in newJobListings)
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
            var mergedList = SeleniumTestsHelpers.MergeJobListings(newJobListings, savedJobListings.JobListingsList);
            SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
        }
        else
        {
            Console.WriteLine($"No new job listings found after comparing live with already existing jobListings on file: {fileName}");
        }
    }

     public async Task CrawlAsynchStartPageForJoblinks_ParseJobLinks_WriteToFile(string url, string selectorXPathForJobEntry,string fileName,IProgress<CrawlProgressReport> progress, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true)
    {
        // Report starting
        progress?.Report(new CrawlProgressReport("Starting crawl process", 0));

        // Step 1: Open and extract job listings
        progress?.Report(new CrawlProgressReport("Extracting job listings from page", 10));
        List<JobListing> liveJobListings = OpenAndExtractJobListings(url, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction, removeParams);
    
        // Step 2: Load saved listings
        progress?.Report(new CrawlProgressReport("Loading saved job listings", 20));
        var savedJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, "JobListings");
    
        // Step 3: Find new listings
        progress?.Report(new CrawlProgressReport("Comparing with existing listings", 30));
        var newJobListings = SeleniumTestsHelpers.ExtractNewJobListings(liveJobListings, savedJobListings.JobListingsList);

        if (newJobListings.Count > 0)
        {
            progress?.Report(new CrawlProgressReport($"Found {newJobListings.Count} new listings to process", 40));
        
            int processedCount = 0;
            foreach (var jobListing in newJobListings)
            {
                // Report current job being processed
                progress?.Report(new CrawlProgressReport(
                    $"Processing job: {jobListing.JobLink}", 
                    40 + (int)((double)processedCount / newJobListings.Count * 50)));
            
                if (delayUserInteraction > 0)
                {
                    await Task.Delay(delayUserInteraction);
                }
            
                var updatedJobListing = OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
            
                processedCount++;
            }
        
            // Merge and save
            progress?.Report(new CrawlProgressReport("Merging with existing listings", 90));
            var mergedList = SeleniumTestsHelpers.MergeJobListings(newJobListings, savedJobListings.JobListingsList);
        
            progress?.Report(new CrawlProgressReport("Writing results to file", 95));
            SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
        
            progress?.Report(new CrawlProgressReport("Completed successfully", 100));
        }
        else
        {
            progress?.Report(new CrawlProgressReport("No new job listings found", 100));
        }
    }
   
public async Task CrawlWithProgressAsync(
    string url, 
    string selectorXPathForJobEntry, 
    string fileName, 
    IProgress<CrawlProgressReport> progress, 
    string addDomainToJobPaths = "", 
    int delayUserInteraction = 0, 
    bool removeParams = true,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Initial progress
        progress?.Report(new CrawlProgressReport("Starting crawl process", 0));
        
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Extract listings
        progress?.Report(new CrawlProgressReport("Loading page", 10));
        var liveJobListings = OpenAndExtractJobListings(url, selectorXPathForJobEntry, 
            addDomainToJobPaths, delayUserInteraction, removeParams);
        
        // Step 2: Load saved
        progress?.Report(new CrawlProgressReport("Loading saved data", 30));
        var savedJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, "JobListings");
        
        // Step 3: Find new
        progress?.Report(new CrawlProgressReport("Comparing listings", 50));
        var newJobListings = SeleniumTestsHelpers.ExtractNewJobListings(liveJobListings, savedJobListings.JobListingsList);

        if (newJobListings.Count > 0)
        {
            // Process each with progress
            for (int i = 0; i < newJobListings.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var jobListing = newJobListings[i];
                int progressPercent = 50 + (int)((double)i / newJobListings.Count * 40);
                
                progress?.Report(new CrawlProgressReport(
                    $"Processing {i+1}/{newJobListings.Count} {fileName}", progressPercent));
                
                 if (delayUserInteraction > 0)
                {
                    await Task.Delay(delayUserInteraction);
                }

                await Task.Delay(delayUserInteraction, cancellationToken);
                
                var updatedJobListing = OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction);
                // Update properties.
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
            }
            
            // Final steps
            progress?.Report(new CrawlProgressReport("Saving results", 95));
            var mergedList = SeleniumTestsHelpers.MergeJobListings(newJobListings, savedJobListings.JobListingsList);
            SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
        }
        
        progress?.Report(new CrawlProgressReport($"Completed crawling: {fileName}", 100));
    }
    catch (OperationCanceledException)
    {
        progress?.Report(new CrawlProgressReport("Cancelled", 0));
        throw;
    }
    catch (Exception ex)
    {
        progress?.Report(new CrawlProgressReport($"Error: {ex.Message}", 0));
        throw;
    }
}    
    
    // Progress report class
    public class CrawlProgressReport
    {
        public string Message { get; }
        public int Percentage { get; }
    
        public CrawlProgressReport(string message, int percentage)
        {
            Message = message;
            Percentage = percentage;
        }
    }
    public void MergeResultFilesToExcelFile(string fileName, string[] files)
    {
        SeleniumTestsHelpers.CreateExcelFromExistingFiles(fileName, files);
    }
    
   public void Dispose()
   {
       driver.Quit();
       driver.Dispose();
    }

    private string ExtractTitle()
    {
        string response = string.Empty;
        var bodyNode = _driver.FindElement(By.XPath("//body"));
        string prompt = $@"extract job title from this text in the same language as the text: {bodyNode.Text}";
        response = ChatGPTSearch(prompt);

        // fallback solutions
        if (string.IsNullOrEmpty(response))
        {
            IWebElement? titleNode = TryFindElement("//h1");
            if (titleNode != null)
            {
                response = titleNode.Text;
            }
            else
            {
                SeleniumTestsHelpers.ExtactDataTestIdjobTitleText(bodyNode.Text);
            }
            Console.WriteLine($"Extracted Title: {response}");
        }
        return response;
    }

   private string ExtractPublishedDate()
    {
        string response = string.Empty;
        var bodyNode = _driver.FindElement(By.XPath("//body"));
        string prompt = $@"extract published date from this text in the same language as the text: {bodyNode.Text}";
        response = ChatGPTSearch(prompt);

        // fallback solution using regex
        if (string.IsNullOrEmpty(response))
        {
            response = SeleniumTestsHelpers.ExtractPublishedInfo(bodyNode.Text);
        }

        Console.WriteLine($"Extracted Published Date: {response}");
        return response;
    }

   private string ExtractEndDate()
    {
        string response = string.Empty;
        var bodyNode = _driver.FindElement(By.XPath("//body"));
        string prompt = $@"extract end date from this text in the same language as the text: {bodyNode.Text}";
        response = ChatGPTSearch(prompt);

        // fallback solution using regex
        if (string.IsNullOrEmpty(response))
        {
            response = SeleniumTestsHelpers.ExtractApplyLatestInfo(bodyNode.Text);
        }

        Console.WriteLine($"Extracted End Date: {response}");
        return response;
    }

    private async Task<string> ChatGPTSearchAsync(string prompt)
    {
        if (_chatService == null)
        {
            Console.WriteLine("Chat service not initialized");
            return string.Empty;
        }

        Console.WriteLine($"Using ChatGPT to extract Info: {prompt}");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30-second timeout
            string response = await _chatService.GetChatResponseAsync(prompt, cts.Token);

            if (string.IsNullOrEmpty(response))
            {
                Console.WriteLine("ChatGPT returned empty response");
                return string.Empty;
            }

            Console.WriteLine($"Successfully received response from ChatGPT");
            return response;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("ChatGPT request timed out");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChatGPT request failed: {ex.Message}");
            return string.Empty;
        }
    }
   private string ChatGPTSearch(string prompt)
   { 
        string response  = string.Empty;
        if (_chatService != null)
        {
            Console.WriteLine($"Using ChatGPT to extract Info, {prompt}");
            try
            {
                return ChatGPTSearchAsync(prompt).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Synchronous wrapper failed for ChatGPT search: {ex}");
                return string.Empty;
            }
        }
        return response;
   }

   public JobListing OpenAndParseJobLink(string url, int delayUserInteraction)
    {
        var jobListing = new JobListing();
        jobListing.JobLink = url;
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
            _driver.SwitchTo().Window(_driver.WindowHandles.Last());
            _driver.Navigate().GoToUrl(url);
            AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            WebDriverWait wait = new(_driver, TimeSpan.FromSeconds(10));
            try
            {
                WaitForDocumentReady(wait);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during WaitForDocumentReady() : {ex.Message}");
            }
            if(BlockedInfoOnPage())
            {
                Console.WriteLine($"Blocked on jobLink page: {url}");
                return jobListing;
            }
            if (url.Contains("linkedin"))
            { 
                ShowMore();
            }
            // extract info on page
            jobListing.Title = ExtractTitle();
            jobListing.ContactInformation = ExtractContactInfo();
            jobListing.Published = ExtractPublishedDate();
            jobListing.EndDate = ExtractEndDate();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning Exception OpenAndParseJobLink({url}) , exception message: {ex.Message}");
        }
        return jobListing;
    }

   private void ShowMore()
    {
        try
        {
            // Wait for the button to be clickable (recommended)
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            try
            {
                // Wait for the button to be clickable using a lambda expression
                IWebElement seeMoreButton = wait.Until(_driver =>
                    _driver.FindElement(By.CssSelector("button[aria-label='Click to see more description']")));
                        
                Console.WriteLine($"Found button area, Displayed: {seeMoreButton.Displayed}, Enabled: {seeMoreButton.Enabled}");

                // Scroll into view if needed
                var statusScroll =((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", seeMoreButton);
                Console.WriteLine($"ExecuteScript for scroll into: {seeMoreButton.Displayed}, Enabled: {seeMoreButton.Enabled}, status scroll {statusScroll}");

                var statusClick = ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", seeMoreButton);
                Console.WriteLine($"Execute script for click, status click: {statusClick}");
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

   private static void WaitForDocumentReady(WebDriverWait wait)
    {
        bool IsDocumentReady = wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
    }

   public List<JobListing> OpenAndExtractJobListings(string url, string selectorXPathForJobEntry, string addDomainToJobPaths, int delayUserInteraction, bool removeParams = true)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
        _driver.SwitchTo().Window(_driver.WindowHandles.Last());
        _driver.Navigate().GoToUrl(url);

        AcceptPopups();
        Thread.Sleep(delayUserInteraction);
        var jobNodes = _driver.FindElements(By.XPath(selectorXPathForJobEntry));
        if (jobNodes.Count == 0)
        {
            Console.WriteLine($"Blocked on start page {url}");
            //Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on start page {url}");
        }

        Console.WriteLine($"Number of job entries found: {jobNodes.Count}");
        List<JobListing> jobListings = new();
        foreach (var node in jobNodes)
        {
            var jobListing = new JobListing();
            jobListing.JobLink = SeleniumTestsHelpers.ExtractHref(addDomainToJobPaths, node, removeParams);
            jobListings.Add(jobListing);
        }

        return jobListings;
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
                    Console.WriteLine($"Setting environment variable: {parts[0].Trim()}");
                }
            }
        }
        else
        {
            Console.WriteLine(".env file not found.");
        }
    }
   public void AcceptPopups()
    {
        try
        {
                var acceptCookiesButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Accept Cookies')]"));
                var accepteraButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Acceptera')]"));
                var approveButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Godkänn')]"));

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
        catch (NoSuchElementException ex)
        {
            Console.WriteLine($"Eception while looking for Popups {ex.Message}");
        }
    }

   public bool BlockedInfoOnPage()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        Console.WriteLine($"BlockedInfoOnPage():");
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
            var elements = _driver.FindElements(By.XPath(xPath));
            if (elements.Count > 0)
            {
                foreach (var element in elements)
                {
                    if (element.Displayed)
                    {
                        Console.WriteLine($"Element is displayed: {element.Text}");
                        Console.WriteLine($"Element found and Displayed, element.TagName: {element.TagName}");
                        var bodyText = GetElementTextOnCurrentPage("//body");
                        Console.WriteLine("Body Text:");
                        Console.WriteLine(bodyText);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Blocked element exists for xPath: {xPath}, but it is not displayed: {element.Text}. xPath: {xPath}");
                    }
                }
            }
        }
        return false;
    }

   private string GetElementTextOnCurrentPage(string xPath)
    {
        try
        {
            var bodyElement = _driver.FindElement(By.XPath(xPath));
            return bodyElement.Text;
        }
        catch (NoSuchElementException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return string.Empty;
        }
    }

   private string ExtractContactInfo()
    {
        string response = string.Empty;
        var bodyNode = _driver.FindElement(By.XPath("//body"));
        string prompt = $@"extract contact information and roles from this text in the same language as the text: {bodyNode.Text}";
        response = ChatGPTSearch(prompt);

        // fallback solutions
        if (string.IsNullOrWhiteSpace(response))
        {
            response = SeleniumTestsHelpers.ExtactContactInfoFromHtml(bodyNode.Text);
        }
        if (string.IsNullOrWhiteSpace(response))
        {
            response = SeleniumTestsHelpers.ExtractPhoneNumbersFromAreaCodeExtractions(bodyNode.Text);
        }

        Console.WriteLine($"Extracted ContactInfo: {response}");
        return response;
    }

   private IWebElement? TryFindElement(string selector)
        {
            try
            {
                return _driver.FindElement(By.XPath(selector));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Selector {selector} not found: {ex.Message}");
                return null;
            }
        }

}
