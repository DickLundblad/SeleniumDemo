using OpenQA.Selenium;
using WebCrawler.Models;

namespace WebCrawler
{
    [TestFixture]
    public class CompanyContactsAPITests
    {
        CompanyContactsAPI _api;

        [OneTimeSetUp]
        public void StartChromeInDebug()
        {
           IWebDriver driverToUse = ChromeDebugger.StartChromeInDebugMode();
           _api  = new CompanyContactsAPI(driverToUse);
        }

        /// <summary>
        /// Add or update job listings to an existing file.
        /// The joblisting items will only contain a NumberOfEmployes
        /// </summary>
        /// <param name="startUrl"></param>
        /// <param name="selectorXPathForJobEntry"></param>
        /// <param name="fileName"></param>
        /// <param name="addDomainToJobPaths"></param>
        /// <param name="delayUserInteraction"></param>
        [Category("live")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[@data-p-stats]","", "allabolag_se_data_sys_program_utveckling_skane", "")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[contains(@class, 'SearchResultCard-card ')]", "", "allabolag_se_data_sys_program_utveckling_skane", "")]

        public void ParseCompanyListingsOnPage(string startUrl, string selectorXPathForJobEntry, string selectorCSS , string fileName, string addDomainToJobPaths, int delayUserInteraction = 0)
        {
            //Foreach JoblLink found on start URL
            List<CompanyListing> jobListingsOnPage = _api.OpenAndExtractCompanyListings(startUrl, selectorXPathForJobEntry, selectorCSS, addDomainToJobPaths, delayUserInteraction);
            Assert.That(jobListingsOnPage.Count, Is.GreaterThan(0), "There should be at least one job listing on the page.");
        }

        [Category("live")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[@data-p-stats]", "allabolag_se_data_sys_program_utveckling_skane", "")]
        public void CrawlStartPageForCompany_Details__WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, string folderPathToResultFiles = "")
        {
            _api.CrawlStartPageForCompany_Details_WriteToFile(url, selectorXPathForJobEntry,"", fileName, addDomainToJobPaths, delayUserInteraction, removeParams);
        }

        [Category("live")]
        [TestCase("Connectitude AB", "CTO", "Joel Fjordén", 2000)]
        [TestCase("Connectitude AB", "CEO", "Richard Houltz", 2000)]
        public void OpenAndParseLinkedInForPeople_WriteToFile(string companyName, string role, string expectedName, int delayUserInteraction = 0)
        {
            var randomName = Guid.NewGuid().ToString("N").Substring(0, 8);
            var fileName = "OpenAndParseLinkedInForPeople_WriteToFile"+ "_" +role + "_" + randomName;
            //var searchUrl = $"https://www.linkedin.com/search/results/people/?keywords={Uri.EscapeDataString(companyName)}&{role}&origin=GLOBAL_SEARCH_HEADER";
            var searchUrl = $"https://www.linkedin.com/search/results/people/?keywords={Uri.EscapeDataString(companyName)}{Uri.EscapeDataString(" ")}{role}&origin=GLOBAL_SEARCH_HEADER";
            _api.ParseLinkeInForPeopleForRole_WriteToFile(searchUrl, companyName, role,fileName, delayUserInteraction);
            var fileAndPath = "LinkedInPeople//" + fileName + ".csv";
            Assert.That(File.Exists(fileAndPath), Is.True, "File should be created after parsing LinkedIn for people.");
        }


        [Category("live")]
        [TestCase(31,"https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&county=Sk%C3%A5ne", "//div[@data-p-stats]", "allabolag_se_data_sys_program_utveckling_skane", "")]
        public void CrawlPageCountForCompany_Details__WriteToFile(int pageCount, string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, string folderPathToResultFiles = "")
        {
            _api.CrawlStartPageForCompany_Details_WriteToFile(url, selectorXPathForJobEntry, "", fileName, addDomainToJobPaths, delayUserInteraction, removeParams);

            for (int i = 2; i <= pageCount; i++)
            {
                string paginatedUrl = $"{url}&page={i}";
                _api.CrawlStartPageForCompany_Details_WriteToFile(paginatedUrl, selectorXPathForJobEntry, "", fileName+"_page_"+i, addDomainToJobPaths, delayUserInteraction, removeParams);
            }
        }

        [Category("live")]
        //[TestCase("merged_filter_emp_and_turnover_applied.csv", "LinkedInPeople",  2000)]
        [TestCase("merged_filter_turnover_100_billion_applied_2025-07-01_14-56-30.csv", "LinkedInPeople", 2000)]//merged_filter_turnover_100_billion_applied_2025-07-01_14-56-30
        public void ParseCompanyFileAndFindLinkedInPeople(string existingFile, string newFileName, int delayUserInteractionMs = 0, int batchSize = 5, int sleepBetweenBatchMs= 1000 * 10)
        {
            string newFile = GenerateFileName(newFileName);
            CompanyListings allCompaniesListings = SeleniumTestsHelpers.LoadCompanyListingsFromFile(existingFile);

            // create result file
            PeopleLinkedInDetails peopleList = new("FilteredCompanies");
            int count = 0;
            foreach (var company in allCompaniesListings.CompanyListingsList)
            {
                count++;
                // load batch, then sleep
                if (count % batchSize == batchSize -1 )
                {
                    Thread.Sleep(1000*10);
                }

                // Parse LinkedIn for people
                var trimmedCompanyName = company.CompanyName.Trim();

                var peopleDetailsCEO = _api.OpenAndParseLinkedInForPeople(trimmedCompanyName, "CEO", delayUserInteractionMs);
                peopleList.PeopleLinkedInDetailsList.AddRange(peopleDetailsCEO);

                var peopleDetailsCTO = _api.OpenAndParseLinkedInForPeople(trimmedCompanyName, "CTO", delayUserInteractionMs);
                peopleList.PeopleLinkedInDetailsList.AddRange(peopleDetailsCTO);
            }
            SeleniumTestsHelpers.WriteToFile(peopleList, newFile);
        }

        [Category("live")]
        [TestCase("Connectitude AB", "https://www.linkedin.com/company/connectitude/")]
        public void FindCompanyOnLinkedIn(string companyName, string expectedUrl)
        {
            // TOD replace with crawl methods
            var res = "https://www.linkedin.com/company/connectitude/";

            Assert.That(res, Is.EqualTo(expectedUrl), "The LinkedIn URL for the company should match the expected URL.");
        }

        [OneTimeTearDown]
        public void CloseChrome() 
        {
            _api?.Dispose();
        }


        private string GenerateFileName(string prefix, string fileEnding = ".csv")
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{prefix}_{timestamp}{fileEnding}";
        }
    }
}
