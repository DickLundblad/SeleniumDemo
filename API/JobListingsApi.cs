using DocumentFormat.OpenXml.Bibliography;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumDemo.Models;
using SeleniumDemo;
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
            TestContext.WriteLine("CHATGPT_API_KEY is not set in the .env file.");
        }
    }

    public void CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true)
    {
        List<JobListing> jobListings = OpenAndExtractJobListings(url, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction, removeParams);
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
            
        var existingJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, "JobListings");
        var mergedList = SeleniumTestsHelpers.MergeJobListings(jobListings, existingJobListings.JobListingsList);
        SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
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
            TestContext.WriteLine($"Extracted Title: {response}");
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

        TestContext.WriteLine($"Extracted Published Date: {response}");
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

        TestContext.WriteLine($"Extracted End Date: {response}");
        return response;
    }

   private string ChatGPTSearch(string prompt)
   { 
        string response  = string.Empty;
        if (_chatService != null)
        {
            TestContext.WriteLine($"Using ChatGPT to extract Info, {prompt}");       
            var task = _chatService.GetChatResponse(prompt);
            if (task != null)
            {
                response = task.Result;
                TestContext.WriteLine($"Result from ChatGPT: {prompt}");
            }
            if (response == string.Empty)
            {
                TestContext.WriteLine($"ChatGPT returned empty response for prompt");
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
                TestContext.WriteLine($"Error during WaitForDocumentReady() : {ex.Message}");
            }
            if(BlockedInfoOnPage())
            {
                TestContext.WriteLine($"Blocked on jobLink page: {url}");
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
            TestContext.WriteLine($"Warning Exception OpenAndParseJobLink({url}) , exception message: {ex.Message}");
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
            Assert.That(BlockedInfoOnPage(), Is.False, $"Blocked on start page {url}");
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
                    TestContext.WriteLine($"Setting environment variable: {parts[0].Trim()}");
                }
            }
        }
        else
        {
            TestContext.WriteLine(".env file not found.");
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
            var elements = _driver.FindElements(By.XPath(xPath));
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

   private string GetElementTextOnCurrentPage(string xPath)
    {
        try
        {
            var bodyElement = _driver.FindElement(By.XPath(xPath));
            return bodyElement.Text;
        }
        catch (NoSuchElementException ex)
        {
            TestContext.WriteLine($"Error: {ex.Message}");
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

        TestContext.WriteLine($"Extracted ContactInfo: {response}");
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
                TestContext.WriteLine($"Selector {selector} not found: {ex.Message}");
                return null;
            }
        }

}
