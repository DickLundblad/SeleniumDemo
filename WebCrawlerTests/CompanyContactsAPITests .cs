using OpenQA.Selenium;
using WebCrawler.Models;
using static JobListingsApi;


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
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "", "SearchResultCard", "allabolag_se_data_sys_program_utveckling_skane", "")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[contains(@class, 'SearchResultCard-card ')]", "", "allabolag_se_data_sys_program_utveckling_skane", "")]

        public void ParseJobListingsOnPage(string startUrl, string selectorXPathForJobEntry, string selectorCSS , string fileName, string addDomainToJobPaths, int delayUserInteraction = 0)
        {
            //Foreach JoblLink found on start URL
            List<CompanyListing> jobListingsOnPage = _api.OpenAndExtractCompanyListings(startUrl, selectorXPathForJobEntry, selectorCSS, addDomainToJobPaths, delayUserInteraction);

        }


        [Category("live")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[@data-p-stats]", "allabolag_se_data_sys_program_utveckling_skane", "")]
        public void CrawlStartPageForCompany_Details__WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, string folderPathToResultFiles = "")
        {
            _api.CrawlStartPageForCompany_Details_WriteToFile(url, selectorXPathForJobEntry,"", fileName, addDomainToJobPaths, delayUserInteraction, removeParams);
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


        [OneTimeTearDown]
        public void CloseChrome() 
        {
            _api?.Dispose();
        }
    }
}
