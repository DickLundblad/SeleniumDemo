using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;
using WebCrawler.Models;

namespace WebCrawler
{
    public class CompaniesLivePagesTests
    {
        private ChromeDriver _driver; // Changed type from IWebDriver to ChromeDriver for improved performance
        private CompanyContactsAPI _companyAPI;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
           _driver = (ChromeDriver)ChromeDebugger.StartChromeInDebugMode();
            _companyAPI = new CompanyContactsAPI(_driver);
        }

        [Category("live")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[@data-p-stats]")]
        public void ValidateThatStartPageIsLoaded(string url, string selectorXPathForJobEntry, int delayUserInteraction = 0)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
            _driver.SwitchTo().Window(_driver.WindowHandles.Last());
            _driver.Navigate().GoToUrl(url);

            _companyAPI.AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            Assert.That(_companyAPI.BlockedInfoOnPage(), Is.False, $"Blocked on page:{url}");

            var jobNodes = _driver.FindElements(By.XPath(selectorXPathForJobEntry));

            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }
     
        [Category("live")]
        [TestCase("Connectitude AB","CTO", "Joel Fjordén", 2000)]
        [TestCase("Connectitude AB", "CEO", "Richard Houltz", 2000)]
        [TestCase("house of test Consulting", "CEO", "Sebastian Thuné", 2000)]
        [TestCase("house of test Consulting", "CEO", "Johan Magnusson", 2000)]
        public void ValidateLinkedInSearchForPeopleWithRoleAtCompany(string companyName, string role, string expectedName, int delayUserInteraction = 2000)
        {
            var searchUrl = $"https://www.linkedin.com/search/results/people/?keywords={Uri.EscapeDataString(companyName)}{Uri.EscapeDataString(" ")}{role}&origin=GLOBAL_SEARCH_HEADER";

            var res = _companyAPI.OpenAndParseLinkedInForPeople(searchUrl, companyName, role, delayUserInteraction);
            Assert.That(res.Count, Is.GreaterThan(0), "No people found");
            Assert.That(res[0].Name, Is.EqualTo(expectedName), "No people found");
        }


        [Category("live")]
        [TestCase("Connectitude AB", "https://www.linkedin.com/company/connectitude/", 4000)]
        [TestCase("QlikTech International AB", "https://www.linkedin.com/company/qliktech-international-ab/", 4000)] // should be QLIK
        [TestCase("Ubisoft Entertainment Sweden AB", "https://www.linkedin.com/company/massiveentertainment/", 4000)]
        [TestCase("Lime Technologies Sweden AB", "https://www.linkedin.com/company/limetechnologies/", 4000)]
        [TestCase("TNGSTN Sweden Services AB", "adsdfsdf", 4000)]
        [TestCase("AppLogic Sweden AB ", "https://www.linkedin.com/company/applogicnetworks/", 4000)]
        [TestCase("Vitec Unikum Datasystem Aktiebolag", "https://www.linkedin.com/company/unikum/", 4000)]
        [TestCase("Axiell Sverige AB", "https://www.linkedin.com/company/axiell-sverige/", 4000)]
        public void ValidateLinkedInSearchForCompany(string companyName, string expectedUrl, int delayUserInteraction = 0)
        {
            var res = _companyAPI.SearchAndReturnCompanyLinkedInPageTryDifferentSubstrings(companyName, delayUserInteraction);
            Assert.That(res, Is.EqualTo(expectedUrl));
        }

        [Category("live")]
        [TestCase("Axiell Sverige AB", "https://www.linkedin.com/company/axiell-sverige/", "http://www.axiell.se", 4000)]
        [TestCase("Lime Technologies Sweden AB", "https://www.linkedin.com/company/limetechnologies/", "https://www.lime-technologies.com/", 4000)]
        public void ValidateCompanyLinkedInDetailsForPage(string companyName, string linkedInUrl, string expectedCompanyWebsite, int delayUserInteraction = 0)
        {
            var res = _companyAPI.CrawlCompanyLinkedInPage(linkedInUrl, companyName, delayUserInteraction);

            Assert.That(res.LinkedInLink, Is.EqualTo(linkedInUrl));
            Assert.That(res.CompanyName, Is.EqualTo(companyName));
            Assert.That(res.CompanyWebsite, Is.EqualTo(expectedCompanyWebsite));
        }


        /*
                  [TestCase("AppLogic Sweden AB ", "https://www.linkedin.com/company/applogicnetworks/", 4000)]
        [TestCase("Vitec Unikum Datasystem Aktiebolag", "https://www.linkedin.com/company/unikum/", 4000)]
        [TestCase("Axiell Sverige AB", "https://www.linkedin.com/company/axiell-sverige/", 4000)]
         */


        //        CrawlCompanyLinkedInPageForUsersWithRole
        [Category("live")]
        [TestCase("https://www.linkedin.com/company/house-of-test-consulting", "House of Test Consulting", "CEO", "Sebastian Thune",4000)]
        [TestCase("https://www.linkedin.com/company/connectitude/", "Connectitude AB", "CEO", "Richard Houltz", 4000)]
        [TestCase("https://www.linkedin.com/company/connectitude/", "Connectitude AB", "CTO", "Joel Fjordén", 4000)]
        [TestCase("https://www.linkedin.com/company/applogicnetworks/", "AppLogic Sweden AB", "CTO", "A", 4000)]
        [TestCase("https://www.linkedin.com/company/unikum/", "Vitec Unikum Datasystem Aktiebolag", "CTO", "B", 4000)]
        [TestCase("https://www.linkedin.com/company/unikum/", "Vitec Unikum Datasystem Aktiebolag", "VD", "Annett Ahrberg", 4000)]
        [TestCase("https://www.linkedin.com/company/axiell-sverige", "Axiell Sverige AB", "CTO", "C", 4000)]
        [TestCase("https://www.linkedin.com/company/axiell-sverige", "Axiell Sverige AB", "CEO", "C", 4000)]
        [TestCase("https://www.linkedin.com/company/axiell-group", "Axiell Group AB", "CEO", "Joel Sommerfeldt", 4000)]
        [TestCase("https://www.linkedin.com/company/axiell-group", "Axiell Group AB", "CEO", "Maria Wasing", 4000)] // CEO not in title, but in description, she has the best match in linnked in search

        public void CrawlCompanyLinkedInPageForUsersWithRole(string linkedInPage, string companyName, string keyWord, string expectedName, int delay)
        {
            List<PeopleLinkedInDetail> matchingProfiles = _companyAPI.CrawlCompanyLinkedInPageForUsersWithRole(linkedInPage, companyName, keyWord, 4000);
            if (matchingProfiles.Count == 0)
            {
                Assert.Fail($"No matching profiles found for company '{companyName}' with role '{keyWord}'.");
            }
            List<PeopleLinkedInDetail> res = _companyAPI.FilterOutMostRelevantMatchForRole(matchingProfiles, companyName, keyWord);

            if (res.Count == 0)
            {   // try again
                companyName = companyName.Replace(" AB", "").Replace(" Aktiebolag", "").Trim();
                res = _companyAPI.FilterOutMostRelevantMatchForRole(matchingProfiles, companyName, keyWord);
                if (res.Count ==0)
                {
                    string[] parts = companyName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        companyName = part.Trim();
                        if (string.IsNullOrEmpty(companyName)) continue;
                       
                        // try again with each part of the company name
                        matchingProfiles = _companyAPI.CrawlCompanyLinkedInPageForUsersWithRole(linkedInPage, companyName, keyWord, 4000);
                        if (matchingProfiles.Count == 0) continue;
                        // filter out most relevant match
                        res = _companyAPI.FilterOutMostRelevantMatchForRole(matchingProfiles, companyName, keyWord);
                        if (res.Count > 0) break;
                    }
                    res = _companyAPI.FilterOutMostRelevantMatchForRole(matchingProfiles, companyName, keyWord);
                }
            }

            Assert.That(res.Count, Is.EqualTo(1));
            Assert.That(res.Any(e => e.Name == expectedName), $"Expected name '{expectedName}' not found in the results.");


        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                _driver?.Quit(); // ensures Chrome and chromedriver processes are terminated
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while quitting _driver: " + ex.Message);
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
    }
}