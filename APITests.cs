using OpenQA.Selenium;


namespace SeleniumDemo
{
    [TestFixture]
    public class APITests
    {
        JobListingsApi _api;

        [OneTimeSetUp]
        public void StartChromeInDebug()
        {
           IWebDriver driverToUse = ChromeDebugger.StartChromeInDebugMode();
           _api  = new JobListingsApi(driverToUse);
        }
        
        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "jobbsafari_se_data_och_it_newest", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]", "se_indeed_empty_what_where", "", 0,false)]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","se_jooble_org", "", 2000)]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", "monster_se_mjukvara_skane", "", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "linkedin_com_it-services-and-it-consulting", "")]
        [TestCase("https://www.linkedin.com/jobs/search/?currentJobId=4205944474&geoId=105117694&keywords=software", "//div[@data-job-id]", "linkedin_com_software", "")]
        public void CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, string folderPathToResultFiles = "")
        {
            _api.CrawlStartPageForJoblinks_ParseJobLinks_WriteToFile(url, selectorXPathForJobEntry, fileName, addDomainToJobPaths, delayUserInteraction, removeParams);
        }

        [OneTimeTearDown]
        public void CloseChrome()
        {
            _api?.Dispose();
        }
    }
}
