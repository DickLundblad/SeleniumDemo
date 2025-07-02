using DocumentFormat.OpenXml.Bibliography;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using WebCrawler;
using WebCrawler.Models;

public class CompanyContactsAPI
{
    private IWebDriver _driver;
    private ChatGPTService? _chatService;
    private const string DEFAULT_SUBFOLDER_CompanyListing = "CompanyListings";
    private const string DEFAULT_SUBFOLDER_LINKEDIN_PEOPLE = "LinkedInPeople";

    public IWebDriver driver => _driver;

    public CompanyContactsAPI(IWebDriver driver)
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

    public void CrawlStartPageForCompany_Details_WriteToFile(string url, string selectorXPathForJobEntry, string selectorCSSPath, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, CancellationToken cancellationToken = new CancellationToken())
    {
        List<CompanyListing> liveJobListings = OpenAndExtractCompanyListings(url, selectorXPathForJobEntry, selectorCSSPath, addDomainToJobPaths, delayUserInteraction, removeParams, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        SeleniumTestsHelpers.WriteListOfCompaniesToFile(liveJobListings, fileName, DEFAULT_SUBFOLDER_CompanyListing);
    }


    public void ParseLinkeInForPeopleForRole_WriteToFile(string url, string companyName, string role, string fileName, int delayUserInteraction = 0)
    {
        var res = OpenAndParseLinkedInForPeople(url, companyName, role, delayUserInteraction); 
        SeleniumTestsHelpers.WriteListOfLinkedInPeopleToFile(res, fileName, DEFAULT_SUBFOLDER_LINKEDIN_PEOPLE);
    }


    public void AcceptPopups()
    {
        try
        {
            var acceptCookiesButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Accept Cookies')]"));
            var accepteraButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Acceptera')]"));
            var approveButton = _driver.FindElement(By.XPath("//button[contains(text(), 'Godk�nn')]"));

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
                "//*[contains(normalize-space(), 'Bekr�fta att du �r en m�nniska')]",
                "//*[contains(normalize-space(), 'Verify you are human')]",
                "//*[contains(text, 'Vi vill f�rs�kra oss om att vi v�nder oss till dig och inte till en robot')]",
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

    public CompanyListing OpenAndParseJobLink(string url, int delayUserInteraction, CancellationToken cancellationToken = new CancellationToken())
    {
        var companyListing = new CompanyListing();
        companyListing.SourceLink = url;
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
            if (BlockedInfoOnPage())
            {
                Console.WriteLine($"Blocked on companyinfo page: {url}");
                return companyListing;
            }
            if (url.Contains("linkedin"))
            {
                ShowMore();
            }
            // extract info on page
            companyListing.Adress = ExtractContactInfo();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning Exception OpenAndParseJobLink({url}) , exception message: {ex.Message}");
        }
        return companyListing;
    }

    public List<CompanyListing> OpenAndExtractCompanyListings(string url, string selectorXPathForEntry = "", string selectorCSS = "", string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
            _driver.SwitchTo().Window(_driver.WindowHandles.Last());
            _driver.Navigate().GoToUrl(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open {url} with driver, check that browser is opened {ex.Message}");
            throw;
        }
        cancellationToken.ThrowIfCancellationRequested();
        AcceptPopups();
        Thread.Sleep(delayUserInteraction);
        var companyNodes = new System.Collections.ObjectModel.ReadOnlyCollection<IWebElement>(new List<IWebElement>());

        if (selectorXPathForEntry != "")
        {
            companyNodes = _driver.FindElements(By.XPath(selectorXPathForEntry));
        }
        else if (selectorCSS != "")
        {
            companyNodes = _driver.FindElements(By.CssSelector(selectorCSS));
        }
        else
        {
            throw new ArgumentException("Either selectorXPathForEntry or selectorCSS must be provided.");
        }
        if (companyNodes.Count == 0)
        {
            Console.WriteLine($"Blocked on start page {url}");
        }

        Console.WriteLine($"Number of entries found: {companyNodes.Count}");
        List<CompanyListing> companyListings = new();
        foreach (var node in companyNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nodeText = node.Text.Trim();
            Console.WriteLine($"# All the text in the Node: {nodeText}");

            var companyListing = new CompanyListing();
            companyListing.SourceLink = SeleniumTestsHelpers.ExtractHref("", node);
            companyListing.Description = ParseCompanyDescriptionFromText(nodeText); //SeleniumTestsHelpers.ExtractOrgNbr(node);
            companyListing.OrgNumber = ParseOrgNbrFromText(nodeText);
            companyListing.TurnoverYear = ParseTurnoverYearFromText(nodeText);
            companyListing.Turnover = ParseTurnoverAmountFromText(nodeText);
            companyListing.NumberOfEmployes = ParseNbrOfEmplyeesFromText(nodeText);
            companyListing.Adress = ParseAdressFromText(nodeText);
            companyListing.CompanyName = ParseCompanyNameFromText(nodeText);
            companyListings.Add(companyListing);

        }

        return companyListings;
    }

    private void OpenChromeInstance()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
        _driver.SwitchTo().Window(_driver.WindowHandles.Last());
    }

    private void OpenPageRemovePopupsLookForBlockedEtc(string url, int delayUserInteraction, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            OpenChromeInstance();
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
            if (BlockedInfoOnPage())
            {
                Console.WriteLine($"Blocked on jobLink page: {url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }


    public List<PeopleLinkedInDetail> CrawlCompanyLinkedInPageForUsersWithRole(string companyLinkedInUrl,string companyName, string role, int delayUserInteraction)
    {
        var res = new List<PeopleLinkedInDetail>();

        // open companyLinkedInUrl/people

        //crawl uses with role

        // return user.


        return res;
    }

    public CompanyLinkedInDetail CrawlCompanyLinkedInPage(string url, string companyName, int delayUserInteraction)
    {
        var res = new CompanyLinkedInDetail();
        res.CompanyName = companyName;
        res.LinkedInLink = url;

        // open url
        CancellationToken cancellationToken = new CancellationToken();
        try
        {
            OpenChromeInstance();
            // crawl company info "${url}/about/"
            // contains about and a link to the company page
            OpenPageRemovePopupsLookForBlockedEtc(url+ "/about", delayUserInteraction, cancellationToken);

            // sort by Companies, the search url for Companies cant be used directly,
            // so pressing the link to [Companies] is the only way to get the correct results
            // The xPath does not work on this frame either so we have to use CSS selector
           /* var filterButtons = driver.FindElements(By.CssSelector("button.search-reusables__filter-pill-button"));
            foreach (var button in filterButtons)
            {
                var btnText = button.Text.Trim();

                if (btnText == "Companies")
                {
                    button.Click();
                    break;
                }
            }
            WebDriverWait wait = new(_driver, TimeSpan.FromSeconds(10));
            var companyLink = wait.Until(d => d.FindElement(By.CssSelector("a[href*='linkedin.com/company/']")));
            var href = companyLink.GetAttribute("href");

            res = href;*/
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        return res;



        //crawl 


        return res;
    }

    public string SearchAndReturnCompanyLinkedInPageTryDifferentSubstrings(string companyName, int delayUserInteraction)
    {
        string res = SearchAndReturnCompanyLinkedInPage(companyName, delayUserInteraction);

        if (string.IsNullOrEmpty(res))
        {
            // try again
            companyName = companyName.Replace(" AB", "").Replace(" Aktiebolag", "").Trim();
            res = SearchAndReturnCompanyLinkedInPage(companyName, delayUserInteraction);
        }
        return res;
    }
    public string SearchAndReturnCompanyLinkedInPage(string companyName, int delayUserInteraction)
    {
        CancellationToken cancellationToken = new CancellationToken();
        string res = "";
        var url = $"https://www.linkedin.com/search/results/all/?keywords={Uri.EscapeDataString(companyName)}";
        try
        {
            OpenChromeInstance();
            OpenPageRemovePopupsLookForBlockedEtc(url, delayUserInteraction, cancellationToken);

            // sort by Companies, the search url for Companies cant be used directly,
            // so pressing the link to [Companies] is the only way to get the correct results
            // The xPath does not work on this frame either so we have to use CSS selector
            var filterButtons = driver.FindElements(By.CssSelector("button.search-reusables__filter-pill-button"));
            foreach (var button in filterButtons)
            {
                var btnText = button.Text.Trim();

                if (btnText == "Companies")
                {
                    button.Click();
                    break;
                }
            }
            WebDriverWait wait = new(_driver, TimeSpan.FromSeconds(10));
            var companyLink = wait.Until(d => d.FindElement(By.CssSelector("a[href*='linkedin.com/company/']")));
            var href = companyLink.GetAttribute("href");

            res = href;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        return res;
    }

    public List<PeopleLinkedInDetail> OpenAndParseLinkedInForPeople(string companyName, string role, int delayUserInteraction, CancellationToken cancellationToken = new CancellationToken())
    {
        // Use the LinkedIn search URL format for people
        string url = $"https://www.linkedin.com/search/results/people/?keywords={Uri.EscapeDataString(companyName)}{Uri.EscapeDataString(" ")}{role}&origin=GLOBAL_SEARCH_HEADER";
        return OpenAndParseLinkedInForPeople(url, companyName, role, delayUserInteraction, cancellationToken);
    }

    public List<PeopleLinkedInDetail> OpenAndParseLinkedInForPeople(string url, string companyName, string role, int delayUserInteraction, CancellationToken cancellationToken = new CancellationToken())
    {

        List<PeopleLinkedInDetail> res = new List<PeopleLinkedInDetail>();

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
            if (BlockedInfoOnPage())
            {
                Console.WriteLine($"Blocked on jobLink page: {url}");
            }
            res = ExtractLinkedInPersons(companyName, role);


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning Exception OpenAndParseJobLink({url}) , exception message: {ex.Message}");
        }
        return res;
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit(); // ensures Chrome and chromedriver processes are terminated
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while quitting driver: " + ex.Message);
        }
        finally
        {
            _driver?.Dispose();
        }
        // also kill andy "zombie" processes that might have been left behind
        try
        {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("chromedriver"))
            {
                process.Kill();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while killling chromedriver processes: " + ex.Message);
        }
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
        string response = string.Empty;
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
                var statusScroll = ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", seeMoreButton);
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

    private string ExtractUsingRegexp(string input, string regExp, int useGroupIndex = 1)
    {
        string res = string.Empty;
        var match = Regex.Match(input, regExp, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            res = match.Groups[useGroupIndex].Value;
            Console.WriteLine("RegExp match : " + res);
        }
        else
        {
            Console.WriteLine($"Regexp returned no match. The regexp was: {regExp}. the input was: {input}");
        }
        return res;
    }


    /// <summary>
    /// Use for https://www.linkedin.com/search/results/all/?keywords=
    /// </summary>
    /// <param name="companyName"></param>
    /// <param name="keyWord"></param>
    /// <returns></returns>
    /// 
    private List<PeopleLinkedInDetail> ExtractLinkedInPersons(string companyName, string keyWord)
    {
        List<PeopleLinkedInDetail> res = new List<PeopleLinkedInDetail>();
        var userCards = _driver.FindElements(By.CssSelector("div[data-view-name='search-entity-result-universal-template']"));
        var companyNameWithoutCompanyForm = TrimCompanyNameWithoutCompanyForm(companyName);

        foreach (var card in userCards)
        {
                try
                {
                    // Extract job title text
                    var titleElement = card.FindElement(By.CssSelector("div.t-14.t-black.t-normal"));
                    var titleText = titleElement.Text;

                    if (titleText.Contains(keyWord, StringComparison.OrdinalIgnoreCase) &&
                        titleText.Contains(companyNameWithoutCompanyForm, StringComparison.OrdinalIgnoreCase))
                    {
                        // Find LinkedIn profile link
                        var linkElement = card.FindElement(By.CssSelector("a[href*='linkedin.com/in/']"));
                        string profileUrl = linkElement.GetAttribute("href");;
                        if (! string.IsNullOrEmpty(profileUrl) && profileUrl.Contains('?'))
                        { 
                            profileUrl = profileUrl.Split('?')[0]; // Remove any query parameters
                        }

                        var mb1Div = card.FindElement(By.CssSelector("div.mb1"));
                        var mb1Text = mb1Div.Text;
                        string nameText = mb1Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        res.Add(new PeopleLinkedInDetail
                        {
                            CompanyName = companyName.Trim(),
                            OrgNumber = string.Empty, // Org number not available in this context
                            LinkedInLink = profileUrl,
                            Email = string.Empty, // Email not available in this context
                            Title = titleText.Trim(),
                            Name = nameText.Trim(),
                            Role = keyWord.Trim()
                        });
                    }
                }
                catch (NoSuchElementException ex)
                {
                    Console.WriteLine($"NoSuchElementException occurred when looking for {companyNameWithoutCompanyForm} with keyWord {keyWord} exception  {ex.Message} ");
                    // Skip cards missing expected elements
                    continue;
                }
        }
        if (res.Count ==0)
        {             
            Console.WriteLine($"No LinkedIn persons found for company: {companyName} with keyword: {keyWord}");
        }
        else
        {
            Console.WriteLine($"Found {res.Count} LinkedIn persons for company: {companyName} with keyword: {keyWord}");
        }

        if (res.Count == 1)
        {
            Console.WriteLine($"Only 1 LinkedIn persons found for company: {companyName} with keyword: {keyWord} was hoping for two");
        }

        return res;
    }


    private string TrimCompanyNameWithoutCompanyForm(string companyName)
    {
        if(companyName.Contains(" AB"))
        {
            companyName = companyName.Remove(companyName.IndexOf(" AB", StringComparison.OrdinalIgnoreCase), 3).Trim();
        }
        if (companyName.Contains(" Aktiebolag"))
        {
            companyName = companyName.Remove(companyName.IndexOf(" Aktiebolag", StringComparison.OrdinalIgnoreCase), 3).Trim();
        }
        return companyName;
    }


    private string ParseCompanyNameFromText(string input)
    {
        string regExp = @"^(.*)";
        string companyName = ExtractUsingRegexp(input, regExp);
        return companyName;
    }

    private string ParseAdressFromText(string input)
    {
        string res = string.Empty;
        // the adress appears after the phonenumbers, so we can use a regex to extract it
        var match = Regex.Match(input, @"Telefon\s*\n[^\n]+\n(.+)", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            res = match.Groups[1].Value.Trim();
        }
        else
        {
            var regExp = @"([A-Z���a-z���]+(?:\s+[A-Z���a-z���]+)*\s+\d+[^\d,]*,\s*\d{3}\s*\d{2}\s+[A-Z���a-z���]+)";
            res  = ExtractUsingRegexp(input, regExp);   
        }
        if (string.IsNullOrEmpty(res))
        {             
            Console.WriteLine($"No address found in the input text: {input}");
        }
        return res; 
    }

    private string ParseOrgNbrFromText(string input)
    {
        string regExp = @"Org\.?nr\s*\n*([0-9]{6}-[0-9]{4})";
        string orgNbr = ExtractUsingRegexp(input, regExp);

        return orgNbr;
    }

    private string ParseCompanyDescriptionFromText(string input)
    {
        // TODO Implement
        string orgNbr = "TODO Implement ParseCompanyDescriptionFromText";

        return orgNbr;
    }

    private int ParseTurnoverYearFromText(string input)
    {
        string regExp = @"OMS�TTNING\s+(\d{4})\s*\n\s*([\d\s]+)";
        string year = ExtractUsingRegexp(input, regExp).Trim();

        if (!string.IsNullOrEmpty(year))
        {
            bool res = int.TryParse(year, out int yearAsInt);

            if (res)
            {
                return yearAsInt;
            }
            else
            {
                Console.WriteLine($"Could not parse turnover amount from string: {year}");
                return 0;
            }
        }
        else
        {
            Console.WriteLine($"No turnover amount found in the input text: {input}");
            return 0;
        }
    }

    private int ParseTurnoverAmountFromText(string input)
    {
        string regExp = @"OMS�TTNING\s+(\d{4})\s*\n\s*([\d\s]+)";
        string turnoverStr = ExtractUsingRegexp(input, regExp, 2);
        if (! string.IsNullOrEmpty(turnoverStr))
        {
            bool res = int.TryParse(turnoverStr.Replace(" ", ""), out int turnoverAmount);

            if (res)
            {
                return turnoverAmount;
            }
            else
            {
                Console.WriteLine($"Could not parse turnover amount from string: {turnoverStr}");
                return 0;
            }
        }
        else
        {
            Console.WriteLine($"No turnover amount found in the input text: {input}");
            return 0;
        }
    }

    private int ParseNbrOfEmplyeesFromText(string input)
    {
        string regExp = @"ANST�LLDA\s*\n\s*(\d+)";
        string nbrOfEmpStr = ExtractUsingRegexp(input, regExp).Trim();

        bool res = int.TryParse(nbrOfEmpStr, out int empNbr);

        if (res)
        {
            return empNbr;
        }
        else
        {
            Console.WriteLine($"Could not parse Emplyees from string: {nbrOfEmpStr}");
            return empNbr;
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
                    Console.WriteLine($"Setting environment variable: {parts[0].Trim()}");
                }
            }
        }
        else
        {
            Console.WriteLine(".env file not found.");
        }
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
