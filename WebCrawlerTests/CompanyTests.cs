using WebCrawler.Models;

namespace WebCrawler
{
    [TestFixture]
    public class CompanyTests
    {

        /// <summary>
        /// Merge all files into one CSV file.
        /// </summary>
        /// <param name="inputFolder"></param>
        [TestCase("CompanyListings")]
        public void MergeAllCVFilesToOne(string inputFolder)
        {
            string testName = "TestMergeAllCVFilesToOne";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{testName}_{timestamp}.csv";
            SeleniumTestsHelpers.MergeAllCVFilesToOne(inputFolder, fileName);
            var filePath = Path.Combine(SeleniumTestsHelpers.GetOutputFolderPath(), fileName);
            Assert.That(File.Exists(filePath), Is.True, $"The merged file {fileName} was not created successfully.");
        }

        [Test]
        public void ValidateThatDuplicatesCompaniesCantBeAddedToCollection()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"ValidateThatDuplicatesCompaniesCantBeAddedToCollection{randomName}";
            var orgId1 = "123456-0000";
            var orgId2 = "123456-1000";
            CompanyListings listings = new CompanyListings(fileName);

            var comp1 = new CompanyListing() { OrgNumber = orgId1 };
            var compDuplicate = new CompanyListing() { OrgNumber = orgId1 };
            var comp2 = new CompanyListing() { OrgNumber = orgId2 };

            listings.InsertIgnoreAlreadyExistingCompany(comp1);
            listings.InsertIgnoreAlreadyExistingCompany(compDuplicate);
            listings.InsertIgnoreAlreadyExistingCompany(comp2);

            Assert.That(listings.CompanyListingsList.Count, Is.EqualTo(2), "The collection should only contain one item after inserting two(2) companies and one(1) duplicate.");
        }

        [Test]
        public void ValidateThatDuplicatesCompaniesCantBeAddedToCollection_WriteToFile()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"ValidateThatDuplicatesCompaniesCantBeAddedToCollection_WriteToFile{randomName}";
            var orgId1 = "123456-0000";
            var orgId2 = "123456-1000";
            CompanyListings listings = new CompanyListings(fileName);

            var comp1 = new CompanyListing() { OrgNumber = orgId1 };
            var compDuplicate = new CompanyListing() { OrgNumber = orgId1 };
            var comp2 = new CompanyListing() { OrgNumber = orgId2 };

            listings.InsertOrUpdate(comp1);
            listings.InsertOrUpdate(compDuplicate);
            listings.InsertOrUpdate(comp2);

            SeleniumTestsHelpers.WriteToFile(listings, fileName);
            var findFileInfo = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{fileName}.*");
            Assert.That(findFileInfo.Length, Is.GreaterThan(0), "The file was not created.");
        }

        [TestCase("merged.csv", "merged_removed_duplicate_OrgNbr")]
        public void FilterExistingFile_RemoveDuplicateOrgNbr(string existingFile, string newFileName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string newFile = $"{newFileName}_{timestamp}.csv";
            CompanyListings allCompaniesListings = SeleniumTestsHelpers.LoadCompanyListingsFromFile(existingFile);

            // read file
            CompanyListings filteredCompanyListings = new("FilteredCompanies", existingFile);
            // use filter on an existing file to create a new file with only the filtered items
            foreach (var company in allCompaniesListings.CompanyListingsList)
            {
                filteredCompanyListings.InsertOrUpdate(company);
            }
            SeleniumTestsHelpers.WriteToFile(filteredCompanyListings, newFile);
        }

        [TestCase("merged.csv", "merged_filter_emp_and_turnover_applied")]
        public void FilterExistingFile_NumberOfEmplyeesAndTurnover(string existingFile, string newFileName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string newFile = $"{newFileName}_{timestamp}.csv";
            CompanyListings allCompaniesListings = SeleniumTestsHelpers.LoadCompanyListingsFromFile(existingFile);

            // read file
            CompanyListings filteredCompanyListings = new("FilteredCompanies", existingFile);
            // use filter on an existing file to create a new file with only the filtered items
            foreach (var company in allCompaniesListings.CompanyListingsList)
            {
                if (company.NumberOfEmployes > 2)
                {
                    if (company.Turnover > 1000)
                    {
                        filteredCompanyListings.InsertOrUpdate(company);
                    }
                    else
                    {
                        Console.WriteLine($"Company {company.CompanyName} with OrgNumber {company.OrgNumber} has Turnover less than 1000 and will not be included in the filtered list.");
                    } 
                }
                else
                {
                    Console.WriteLine($"Company {company.CompanyName} with OrgNumber {company.OrgNumber} has less than 2 employees and will not be included in the filtered list.");
                }
            }
            SeleniumTestsHelpers.WriteToFile(filteredCompanyListings, newFile);
        }

    }
}
