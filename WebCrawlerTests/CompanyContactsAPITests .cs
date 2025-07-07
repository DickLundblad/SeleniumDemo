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
            _api = new CompanyContactsAPI(driverToUse);
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
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[@data-p-stats]", "", "allabolag_se_data_sys_program_utveckling_skane", "")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[contains(@class, 'SearchResultCard-card ')]", "", "allabolag_se_data_sys_program_utveckling_skane", "")]

        public void ParseCompanyListingsOnPage(string startUrl, string selectorXPathForJobEntry, string selectorCSS, string fileName, string addDomainToJobPaths, int delayUserInteraction = 0)
        {
            //Foreach JoblLink found on start URL
            List<CompanyListing> jobListingsOnPage = _api.OpenAndExtractCompanyListings(startUrl, selectorXPathForJobEntry, selectorCSS, addDomainToJobPaths, delayUserInteraction);
            Assert.That(jobListingsOnPage.Count, Is.GreaterThan(0), "There should be at least one job listing on the page.");
        }


        [Category("live")]
        [TestCase("https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&page=1&county=Sk%C3%A5ne", "//div[@data-p-stats]", "allabolag_se_data_sys_program_utveckling_skane", "")]
        [TestCase("https://www.allabolag.se/segmentering?revenueFrom=100000&revenueTo=1000000&location=Sk%C3%A5ne", "//div[contains(@class, 'SegmentationSearchResultCard-card')]", "allabolag_se_100_miljon_till_1000_miljoner_skane", "")]
        public void CrawlStartPageForCompany_Details__WriteToFile(string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, string folderPathToResultFiles = "")
        {
            _api.CrawlStartPageForCompany_Details_WriteToFile(url, selectorXPathForJobEntry, "", fileName, addDomainToJobPaths, delayUserInteraction, removeParams);
        }

        [Category("live")]
        [TestCase("https://www.linkedin.com/company/connectitude/","Connectitude AB", "CTO", "Joel Fjordén", 2000)]
        [TestCase("https://www.linkedin.com/company/connectitude/", "Connectitude AB", "CEO", "Richard Houltz", 2000)]
        public void OpenAndParseLinkedInForCompanyPeople(string linkedinCompanyUrl,string companyName, string role, string expectedName, int delayUserInteraction = 0)
        {
            var searchUrl = $"";
            var users = _api.CrawlCompanyLinkedInPageForUsersWithRole(linkedinCompanyUrl, companyName, role, delayUserInteraction);
            Assert.That(users.Count(), Is.AtLeast(1), $"There should be at least one user with role {role} found on the LinkedIn page.");
            Assert.That(users.Any(p => p.Name == expectedName), $"There should be at least one user with name {expectedName} found on the LinkedIn page.");
        }

        [Category("live")]
        [TestCase("Connectitude AB", "CTO", "Joel Fjordén", 2000)]
        [TestCase("Connectitude AB", "CEO", "Richard Houltz", 2000)]
        public void OpenAndParseLinkedInForPeople_WriteToFile(string companyName, string role, string expectedName, int delayUserInteraction = 0)
        {
            var randomName = Guid.NewGuid().ToString("N").Substring(0, 8);
            var fileName = "OpenAndParseLinkedInForPeople_WriteToFile" + "_" + role + "_" + randomName;
            var searchUrl = $"https://www.linkedin.com/search/results/people/?keywords={Uri.EscapeDataString(companyName)}{Uri.EscapeDataString(" ")}{role}&origin=GLOBAL_SEARCH_HEADER";
            _api.ParseLinkeInForPeopleForRole_WriteToFile(searchUrl, companyName, role, fileName, delayUserInteraction);
            var fileAndPath = "LinkedInPeople//" + fileName + ".csv";
            Assert.That(File.Exists(fileAndPath), Is.True, "File should be created after parsing LinkedIn for people.");
        }

        [Category("ResultFiles")]
        [Category("live")]
        [TestCase(31, "https://www.allabolag.se/bransch-s%C3%B6k?q=Datautveckling%2C%20systemutveckling%2C%20programutveckling&county=Sk%C3%A5ne", "//div[@data-p-stats]", "allabolag_se_data_sys_program_utveckling_skane", "")]
        [TestCase(142, "https://www.allabolag.se/segmentering?revenueFrom=100000&revenueTo=1000000&location=Sk%C3%A5ne", "//div[contains(@class, 'SegmentationSearchResultCard-card')]", "allabolag_se_100_miljon_till_1000_miljoner_skane", "")]
        [TestCase(204, "https://www.allabolag.se/segmentering?revenueFrom=100000&revenueTo=1000000&location=V%C3%A4stra%20G%C3%B6taland", "//div[contains(@class, 'SegmentationSearchResultCard-card')]", "allabolag_se_100_miljon_till_1000_miljoner_vastra_gotaland", "")]
        public void CrawlPageCountForCompany_Details__WriteToFile(int pageCount, string url, string selectorXPathForJobEntry, string fileName, string addDomainToJobPaths = "", int delayUserInteraction = 0, bool removeParams = true, string folderPathToResultFiles = "")
        {
            _api.CrawlStartPageForCompany_Details_WriteToFile(url, selectorXPathForJobEntry, "", fileName, addDomainToJobPaths, delayUserInteraction, removeParams);

            for (int i = 2; i <= pageCount; i++)
            {
                string paginatedUrl = $"{url}&page={i}";
                _api.CrawlStartPageForCompany_Details_WriteToFile(paginatedUrl, selectorXPathForJobEntry, "", fileName + "_page_" + i, addDomainToJobPaths, delayUserInteraction, removeParams);
            }
        }


        [Category("ResultFiles")]
        [Category("live")]
        [TestCase("AllCompanies_100_miljoner_till_1_miljard.csv", "ParseCompanyFileAndFindLinkedInPeople", "LinkedInPeople", 2000, 5, 10, 62, 64)]
        public void ParseCompanyFileAndFindLinkedInPeopleForNotAllreadyFound(string existingCompanyFile, string existingPeopleLinkedinFolder, string newFileName = "LinkedInPeople", int delayUserInteractionMs = 0, int batchSizeCrawlLinkedIn = 5, int writeToFileAfterNbrOfCompanies = 10, int startAtPercentOfFile = 0, int stopAtPercentOfFile = 0)
        {
            string[] keyWords = { "CEO", "CTO", "VD", "vVD" };

            PeopleLinkedInDetails peopleDetails = new  PeopleLinkedInDetails(newFileName);
            List<CompanyListing> companiesToCrawl = GetCompaniesToCrawl(existingCompanyFile, startAtPercentOfFile, stopAtPercentOfFile);

            // Load PeopleLinkedInDetails from file and filter out companies that already have people listed
            PeopleLinkedInDetails allAlreadyExistingPeopleDetails = SeleniumTestsHelpers.LoadPeoplesFromFolder(existingPeopleLinkedinFolder);
            //

            int count = 0;
            int fileCounter = 1;

            foreach (var company in companiesToCrawl)
            {
                if (allAlreadyExistingPeopleDetails.PeopleLinkedInDetailsList.Any(p => p.CompanyName.Trim() == company.CompanyName.Trim()))
                {
                    // Skip companies that already have people listed
                    continue;
                }

                count++;

                // Parse LinkedIn for people for different keywords
                var trimmedCompanyName = company.CompanyName.Trim();

                for (int i = 0; i < keyWords.Length; i++)
                {
                    var peopleDetailList = _api.OpenAndParseLinkedInForPeople(trimmedCompanyName, keyWords[i], delayUserInteractionMs);
                    if (peopleDetailList.Count > 0)
                    {
                        // HACK se that correct method is used to add people details
                        peopleDetails.PeopleLinkedInDetailsList.AddRange(peopleDetails.PeopleLinkedInDetailsList);
                    }
                }

                // Write to file after a certain number of companies
                if (count % writeToFileAfterNbrOfCompanies == writeToFileAfterNbrOfCompanies - 1 && peopleDetails.PeopleLinkedInDetailsList.Count > 0)
                {
                    var batchfileName = $"{GenerateFileName(newFileName)}_{fileCounter}";
                    SeleniumTestsHelpers.WriteToFile(peopleDetails, batchfileName);
                    peopleDetails.PeopleLinkedInDetailsList.Clear(); // Clear the list after writing to file
                    fileCounter++;
                }
            }
            if (peopleDetails.PeopleLinkedInDetailsList.Count > 0)
            {
                SeleniumTestsHelpers.WriteToFile(peopleDetails, $"{GenerateFileName(newFileName)}_{fileCounter}");
            }
        }


        [Category("ResultFiles")]
        [Category("live")]
        //[TestCase("merged_filter_emp_and_turnover_applied.csv", "LinkedInPeople",  2000)]
        //[TestCase("TestMergeAllCVFilesToOne_2025-07-03_14-08-21.csv", "LinkedInPeople", 2000)]//merged_filter_turnover_100_billion_applied_2025-07-01_14-56-30
        [TestCase("AllCompanies_100_miljoner_till_1_miljard.csv", "LinkedInPeople", "ParseCompanyFileAndFindLinkedInPeople",  2000, 5,10,60,61)]//merged_filter_turnover_100_billion_applied_2025-07-01_14-56-30
        public void ParseCompanyFileAndFindLinkedInPeople(string existingFile, string newFileName = "LinkedInPeople", string subFolder = "ParseCompanyFileAndFindLinkedInPeople", int delayUserInteractionMs = 0, int batchSizeCrawlLinkedIn = 5, int writeToFileAfterNbrOfCompanies = 10, int startAtPercentOfFile = 0, int stopAtPercentOfFile = 0 )
        {
            string[] keyWords = { "CEO", "CTO", "VD", "vVD" };
            PeopleLinkedInDetails peopleList = new("FilteredCompanies");
            var companiesToCrawl = GetCompaniesToCrawl(existingFile, startAtPercentOfFile, stopAtPercentOfFile);
            int count = 0;
            int fileCounter = 1;

            foreach (var company in companiesToCrawl)
            {
                count++;
                // Parse LinkedIn for people for different keywords
                var trimmedCompanyName = company.CompanyName.Trim();

                for (int i = 0; i < keyWords.Length; i++)
                {
                    var peopleDetails = _api.OpenAndParseLinkedInForPeople(trimmedCompanyName, keyWords[i], delayUserInteractionMs);
                    if (peopleDetails.Count > 0)
                    {
                        peopleList.PeopleLinkedInDetailsList.AddRange(peopleDetails);
                    }
                }

                // Write to file after a certain number of companies
                if (count % writeToFileAfterNbrOfCompanies == writeToFileAfterNbrOfCompanies - 1 && peopleList.PeopleLinkedInDetailsList.Count > 0)
                {
                    var batchfileName = $"{GenerateFileName(newFileName)}_{fileCounter}";
                    SeleniumTestsHelpers.WriteToFile(peopleList, batchfileName);
                    peopleList.PeopleLinkedInDetailsList.Clear(); // Clear the list after writing to file
                    fileCounter++;
                }
            }
            SeleniumTestsHelpers.WriteToFile(peopleList, $"{GenerateFileName(newFileName)}_{fileCounter}",subFolder);
        }


        [OneTimeTearDown]
        public void CloseChrome() 
        {
            _api?.Dispose();
        }

        private static List<CompanyListing> GetCompaniesToCrawl(string existingFile, int startAtPercentOfFile, int stopAtPercentOfFile)
        {
            CompanyListings allCompaniesListings = SeleniumTestsHelpers.LoadCompanyListingsFromFile(existingFile);
            if (allCompaniesListings.CompanyListingsList.Count() == 0)
            {
                Assert.Fail($"No companies found in the provided file {existingFile}. Please check the file path and content.");
            }
            int nbrOfCompanies = allCompaniesListings.CompanyListingsList.Count();
            int startIndex = 0;
            int stopIndex = nbrOfCompanies; // default to the end of the list

            if (startAtPercentOfFile > 0)
            {
                startIndex = (int)(nbrOfCompanies * (startAtPercentOfFile / 100.0));
            }
            if (stopAtPercentOfFile > 0)
            {
                stopIndex = (int)(nbrOfCompanies * (stopAtPercentOfFile / 100.0));
            }


            List<CompanyListing> allCompanies = allCompaniesListings.CompanyListingsList.OrderBy(e => e.CompanyName).ToList<CompanyListing>();
            var companiesToCrawl = allCompanies.GetRange(startIndex, stopIndex - startIndex);

            return companiesToCrawl;
        }
        private string GenerateFileName(string prefix)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{prefix}_{timestamp}";
        }

        private string GenerateFileNameWithEnding(string prefix, string fileEnding = ".csv")
        {
            string fileName = GenerateFileName(prefix);
            return $"{fileName}{fileEnding}";
        }
    }
}
