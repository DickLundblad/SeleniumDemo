using DocumentFormat.OpenXml.Bibliography;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebCrawler.Models;
using WebCrawler;

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

    public string CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, CancellationToken cancellationToken = new CancellationToken())
    {
        List<JobListing> liveJobListings = OpenAndExtractJobListings(url, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction, removeParams, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var savedJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, "JobListings");
        var newJobListings = SeleniumTestsHelpers.ExtractUniqueJobListings(liveJobListings, savedJobListings.JobListingsList);
        var exisingJobLisingsToUpdate = SeleniumTestsHelpers.GetJobListingsToUpdate(savedJobListings.JobListingsList);
        string returnMessage = ""; 
        // process new Joblinks
        if (newJobListings.Count >0)
        {
            Console.WriteLine($"Number of new job listings to open and parse: {newJobListings.Count}");
            foreach (var jobListing in newJobListings)
            {
                Thread.Sleep(delayUserInteraction);
                cancellationToken.ThrowIfCancellationRequested();
                var updatedJobListing = OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction, cancellationToken);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
                jobListing.Refresh = false;
            }
            var mergedList = SeleniumTestsHelpers.MergeJobListingsIgnoreAlreadyExisting(newJobListings, savedJobListings.JobListingsList);
            SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
            returnMessage += $"Existing nbr of JobListings was {savedJobListings.JobListingsList.Count}, adding {newJobListings.Count}";
        }
        else
        {
            returnMessage = $"No new job listings found after comparing live with already existing jobListings on file: {fileName}";
            Console.WriteLine($"No new job listings found after comparing live with already existing jobListings on file: {fileName}");
        }

        // process existing jobLinks that needs to be update
        if (exisingJobLisingsToUpdate.Count >0)
        {
            Console.WriteLine($"Number of existing job listings to update: {exisingJobLisingsToUpdate.Count}");
            foreach (var jobListing in exisingJobLisingsToUpdate)
            {
                Thread.Sleep(delayUserInteraction);
                cancellationToken.ThrowIfCancellationRequested();
                var updatedJobListing = OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction, cancellationToken);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
                jobListing.Refresh = false;
            }
            var mergedList = SeleniumTestsHelpers.MergeJobListingsOverWriteAlreadyExisting(newJobListings, savedJobListings.JobListingsList);
            SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
            returnMessage += $" Existing nbr of JobListings was {savedJobListings.JobListingsList.Count}, updating {exisingJobLisingsToUpdate.Count} of them since they were marked for update";
        }

        return returnMessage;
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

   public JobListing OpenAndParseJobLink(string url, int delayUserInteraction,  CancellationToken cancellationToken = new CancellationToken())
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
            cancellationToken.ThrowIfCancellationRequested();
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

   public List<JobListing> OpenAndExtractJobListings(string url, string selectorXPathForJobEntry, string addDomainToJobPaths, int delayUserInteraction, bool removeParams = true, CancellationToken cancellationToken = new CancellationToken())
    {
        try{
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
            _driver.SwitchTo().Window(_driver.WindowHandles.Last());
            _driver.Navigate().GoToUrl(url);
        }catch (Exception ex) 
        {
            Console.WriteLine($"Could not open {url} with driver, check that browser is opened {ex.Message}");
            throw;
        }
        cancellationToken.ThrowIfCancellationRequested();
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
            cancellationToken.ThrowIfCancellationRequested();
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
