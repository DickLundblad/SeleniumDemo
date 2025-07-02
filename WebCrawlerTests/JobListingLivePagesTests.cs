using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebCrawler.Models;

namespace WebCrawler
{
    public class JobListingLivePagesTests
    {
        private ChromeDriver _driver; // Changed type from IWebDriver to ChromeDriver for improved performance
        private JobListingsApi _api;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
           _driver = (ChromeDriver)ChromeDebugger.StartChromeInDebugMode();
            _api = new JobListingsApi(_driver);
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]")]   
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]")]
        public void ValidateThatStartPageIsLoaded(string url, string selectorXPathForJobEntry, int delayUserInteraction = 0)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.open();");
            _driver.SwitchTo().Window(_driver.WindowHandles.Last());
            _driver.Navigate().GoToUrl(url);

            _api.AcceptPopups();
            Thread.Sleep(delayUserInteraction);
            Assert.That(_api.BlockedInfoOnPage(), Is.False, $"Blocked on page:{url}");

            var jobNodes = _driver.FindElements(By.XPath(selectorXPathForJobEntry));

            Assert.That(jobNodes.Count, Is.GreaterThan(0), "No job entries found on the page.");
            TestContext.WriteLine($"Number of job entries found: {jobNodes.Count}");
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]", "")]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]", "")]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", "", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "")]
        public void ValidateThatJoblinksCanBeRetrievedFromStartPages(string url, string selectorXPathForJobEntry, string addDomainToJobPaths = "", int delayUserInteraction = 0)
        {
            List<JobListing> jobListings = _api.OpenAndExtractJobListings(url, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction);
            foreach (var job in jobListings)
            {
                Assert.That(job.JobLink, Is.Not.Empty, "Job link is empty");
            }
        }

        /// <summary>
        /// Open start page
        /// Extract URLs from the page
        /// Loop over URL and extract Job information
        /// Open any existing result file
        /// Merge results with existing file
        /// </summary>
        /// <param name="url"></param>
        /// <param name="selectorXPathForJobEntry"></param>
        /// <param name="fileName"></param>
        /// <param name="addDomainToJobPaths"></param>
        /// <param name="delayUserInteraction"></param>
        [Category("live")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "jobbsafari_se_data_och_it_newest", "https://jobbsafari.se")]
        [TestCase("https://se.indeed.com/?from=jobsearch-empty-whatwhere", "//*[starts-with(@data-testid, 'slider_item')]", "se_indeed_empty_what_where", "", 0,false)]
        [TestCase("https://se.jooble.org/SearchResult", "//*[starts-with(@data-test-name, '_jobCard')]","se_jooble_org", "", 2000)]
        [TestCase("https://www.monster.se/jobb/sok?q=mjukvara&where=Sk%C3%A5ne&page=1&so=m.s.lh", "//*[@data-testid='jobTitle']", "monster_se_mjukvara_skane", "", 2000)]
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "linkedin_com_it-services-and-it-consulting", "")]
        [TestCase("https://www.linkedin.com/jobs/search/?currentJobId=4205944474&geoId=105117694&keywords=software&origin=JOB_SEARCH_PAGE_SEARCH_BUTTON&refresh=true", "//div[@data-job-id]", "linkedin_com_software", "")]
        public void ValidateThatJoblinksCanBeRetrievedAndParsed_WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true)
        {
            List<JobListing> jobListings = _api.OpenAndExtractJobListings(url, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction, removeParams);
            //loop over each jobListing, open link and extract info
            foreach (var jobListing in jobListings)
            {
                Thread.Sleep(delayUserInteraction);
                var updatedJobListing = _api.OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
            }
            
            var existingJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, "JobListings");
            var mergedList = SeleniumTestsHelpers.MergeJobListingsIgnoreAlreadyExisting(jobListings, existingJobListings.JobListingsList);
            SeleniumTestsHelpers.WriteListOfJobsToFile(mergedList, fileName, "JobListings");
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
        [TestCase("https://www.linkedin.com/jobs/collections/it-services-and-it-consulting", "//div[@data-job-id]", "linkedin_com_it-services-and-it-consulting", "")]
        [TestCase("https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", "//li[starts-with(@id, 'jobentry-')]", "jobbsafari_se_data_och_it_newest", "https://jobbsafari.se")]
        public void AddOrUpdateJobListingsToExistingFile(string startUrl,string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths, int delayUserInteraction = 0)
        {
            var subFolder = "JobListings";

            //Foreach JoblLink found on start URL
            List<JobListing> jobListingsOnPage = _api.OpenAndExtractJobListings(startUrl, selectorXPathForJobEntry, addDomainToJobPaths, delayUserInteraction);
            JobListings existingJobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, subFolder);

            //loop over each jobListing and insert the jobListing
            foreach (var newJob in jobListingsOnPage)
            { 
                existingJobListings.InsertOrUpdate(newJob);
            }
            SeleniumTestsHelpers.WriteToFile(existingJobListings, fileName, subFolder);
        }

       /// <summary>
       /// Open a result file, parse the job links and update the job listings in the file.
       /// </summary>
       /// <param name="fileName"></param>
       /// <param name="delayUserInteraction"></param>
       /// <param name="onlyUpdateMissingContactInfo"></param>
       [Category("live")]
       [TestCase("se_indeed_empty_what_where", 3000)]
       [TestCase("jobbsafari_se_data_och_it_newest")]
       [TestCase("jobbsafari_se_data_och_it_newest",0,true)]
       public void ParseJobLinksAndUpdateJobListingsInExistingFile(string fileName, int delayUserInteraction = 0,bool onlyUpdateMissingContactInfo = false)
        {
            // load JobListings from file
            var subFolder = "JobListings";
           
            JobListings jobListings = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName, subFolder);
            // select only the lines with empty contactInfo
            List<JobListing> listToUpdate = jobListings.JobListingsList;
            if (onlyUpdateMissingContactInfo)
            { 
                listToUpdate = jobListings.JobListingsList
                    .Where(x => x.ContactInformation == null || x.ContactInformation == "")
                    .ToList();
            }
            foreach (var jobListing in listToUpdate)
            {
                Thread.Sleep(delayUserInteraction);
                var updatedJobListing = _api.OpenAndParseJobLink(jobListing.JobLink, delayUserInteraction);
                jobListing.Title = updatedJobListing.Title;
                jobListing.Published = updatedJobListing.Published;
                jobListing.EndDate = updatedJobListing.EndDate;
                jobListing.ContactInformation = updatedJobListing.ContactInformation;
                jobListing.Description = updatedJobListing.Description;
                jobListing.ApplyLink = updatedJobListing.ApplyLink;
            }
            if (listToUpdate.Count > 0)
            { 
                jobListings.JobListingsList.AddRange(listToUpdate);
                SeleniumTestsHelpers.WriteListOfJobsToFile(jobListings.JobListingsList, fileName, "JobListings");
            }
        }

        [Category("live")]
        [TestCase("https://jobbsafari.se/jobb/digital-radio-system-designer-sesri-19207406", 0)]
        [TestCase("https://www.linkedin.com/jobs/view/4194781616/?eBP=BUDGET_EXHAUSTED_JOB&refId=wqmOM1Whbos%2BqR2hax6d%2BQ%3D%3D&trackingId=Y31jWZzmfvJYm7mUln7UBQ%3D%3D&trk=flagship3_job_collections_leaf_page", 0)]
        [TestCase("https://www.linkedin.com/jobs/view/4204957407/?trk=flagship3_search_srp_jobs", 0)]
        [TestCase("https://se.jooble.org/desc/-154934751721925931?ckey=NONE&rgn=-1&pos=1&elckey=3819297206643930044&pageType=20&p=1&jobAge=2608&relb=140&brelb=100&bscr=112&scr=156.8&premImp=1", 0)]
        [TestCase("https://se.jooble.org/desc/-2750184788513872086?ckey=NONE&rgn=-1&pos=3&elckey=3819297206643930044&pageType=20&p=1&jobAge=766&relb=100&brelb=100&bscr=88.1224&scr=88.1224", 2000)]
        [TestCase("https://jobbsafari.se/jobb/solution-architect-intralogistics-development-supply-chain-development-siske-19207507", 0)]
        [TestCase("https://jobbsafari.se/jobb/rd-specialist-till-essentias-protein-solutions-sesmp-19206771", 0)]
        [TestCase("https://www.monster.se/jobberbjudande/it-tekniker-till-internationellt-f%C3%B6retag-malm%C3%B6-sk%C3%A5ne--24828633-9781-4533-95c8-6dc9c2758f21?sid=755339e0-795d-402e-b468-2e6ca4790ae9&jvo=m.mp.s-svr.1&so=m.s.lh&hidesmr=1", 2000)]
        public void ValidateThatAJobLinkCanBeOpenedAndParsed(string url, int delayUserInteraction = 0)
        {
            var jobListing = _api.OpenAndParseJobLink(url, delayUserInteraction);
            // NumberOfEmployes can change if it's a re-direct, but we will keep the original URL
            Assert.That(jobListing.JobLink, Is.EqualTo(url), "Job link is not url");
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