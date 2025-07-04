using OpenQA.Selenium.DevTools.V133.Network;
using WebCrawler.Mappers;
using WebCrawler.Models;

namespace WebCrawler
{
    public class ResultFileOperationsTests
    {
        // write objects to file (Write)
        [Test]
        public void CreateJobListings()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"CreateJobListings_{randomName}";
            var startPage = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting";
            JobListings jobListings = new JobListings(fileName, startPage);
            var job1 = new JobListing() { JobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433" };
            jobListings.InsertOrUpdate(job1);
            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);
            var findFileInfo = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{fileName}.*");
            Assert.That(findFileInfo.Length, Is.GreaterThan(0), "The file was not created.");
        }

        [Test]
        public void ReadJobListings()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"CreateJobListings_{randomName}";
            var jobLink = "https://www.linkedin.com/link1";
            var startPage = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting";
            JobListings jobListings = new JobListings(fileName, startPage);
 
            var job1 = new JobListing() { JobLink = jobLink };
            jobListings.InsertOrUpdate(job1);

            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);
            JobListings jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);

            Assert.That(jobListingsFromFile.JobListingsList.Count, Is.EqualTo(jobListings.JobListingsList.Count), "The re-loaded JobListing from file does not contain the same number of objects.");
            Assert.That(jobListingsFromFile.JobListingsList[0].JobLink, Is.EqualTo(jobLink), "The re-loaded JobListing from file does not contain the same object.");
            Assert.That(jobListingsFromFile.JobListingsList, Is.EquivalentTo(jobListings.JobListingsList).Using(new JobListingComparer()),
                "The re-loaded JobListings from file do not match. the jobLink is used for comparison ");
        }

        [Test]
        public void ReadJobListingsThatShouldBeUpdated()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"CreateJobListings_{randomName}";
            var jobLink = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            JobListings jobListings = new JobListings(fileName);
 
            var job1 = new JobListing() { JobLink = jobLink, Refresh= false};
            var job2 = new JobListing() { JobLink = jobLink2 , Refresh= true};
            jobListings.InsertOrUpdate(job1);
            jobListings.InsertOrUpdate(job2);

            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);
            JobListings jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            List<JobListing> jobListingsToUpdate = SeleniumTestsHelpers.GetJobListingsToUpdate(jobListingsFromFile.JobListingsList);

            Assert.That(jobListingsFromFile.JobListingsList.Count, Is.EqualTo(2), "Two jobListings was saved to file");
            Assert.That(jobListingsToUpdate.Count, Is.EqualTo(1), "Only 1 jobListing was set to be updated");
            Assert.That(jobListingsToUpdate.FirstOrDefault().JobLink, Is.EqualTo(jobLink2), "joblink was not correct");
        }

        [Test]
        public void UpdateJobListings()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"UpdateJobListings_{randomName}";
            var startPage = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting";
            var jobLink = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            var updatedJobLink = "https://www.linkedin.com/updatedlink";
            JobListings jobListings = new JobListings(fileName, startPage);
            var job1 = new JobListing() { JobLink = jobLink };
            var job2 = new JobListing() { JobLink = jobLink2 };
            jobListings.InsertOrUpdate(job1);
            jobListings.InsertOrUpdate(job2);
            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);

            JobListings jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            jobListingsFromFile.JobListingsList[0].JobLink = updatedJobLink;

            SeleniumTestsHelpers.WriteToFile(jobListingsFromFile, fileName);

            JobListings updatedJobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);

            Assert.That(updatedJobListingsFromFile.JobListingsList.Count, Is.EqualTo(jobListings.JobListingsList.Count), "The re-loaded JobListing from file does not contain the same number of objects.");
            Assert.That(updatedJobListingsFromFile.JobListingsList[0].JobLink, Is.EqualTo(updatedJobLink), "The re-loaded JobListing from file does not contain the same object.");
            Assert.That(updatedJobListingsFromFile.JobListingsList, Is.Not.EquivalentTo(jobListings.JobListingsList), "The re-loaded updated JobListings from file should not match. the jobLink is used for comparison ");
        }

        // if jobList exist, don't add, just update the object
        [Test]
        public void UpdateExistingJobListing()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"UpdateExistingJobListings_{randomName}";
            var startPage = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting";
            var contactInformation1 = "Original Contact Information 1";
            var contactInformation2 = "Original Contact Information 2";
            var updatedContactInformation = "updated Contact Information";
            var jobLink1 = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            JobListings jobListings = new JobListings(fileName, startPage);
            var job1 = new JobListing() { JobLink = jobLink1, ContactInformation = contactInformation1 };
            var job2 = new JobListing() { JobLink = jobLink2, ContactInformation = contactInformation2 };
            jobListings.InsertOrUpdate(job1);
            jobListings.InsertOrUpdate(job2);
            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);

            JobListings jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            // Create a new JobListing object with the same jobLink as job1 but different contact information
            var itemToUpdate = new JobListing() { JobLink = jobLink1, ContactInformation = updatedContactInformation };
            itemToUpdate.ContactInformation = updatedContactInformation;
            jobListingsFromFile.InsertOrUpdate(itemToUpdate);
            SeleniumTestsHelpers.WriteToFile(jobListingsFromFile, fileName);

            JobListings updatedJobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            var updatedItem = updatedJobListingsFromFile.JobListingsList.FirstOrDefault(x => x.JobLink == jobLink1);

            Assert.That(updatedItem, Is.EqualTo(job1).Using(new JobListingComparer()), " The re-loaded JobListing from file does not contain the same object.");
            Assert.That(updatedItem.ContactInformation, Is.EqualTo(updatedContactInformation), "The updated item did not have the correct Adress");
        }

        [Test]
        public void MergeJobListingsIgnoreAlreadyExisting()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"UpdateExistingJobListings_{randomName}";
            var contactInformation1 = "Original Contact Information 1";
            var updatedContactInformation = "updated Contact Information";
            var jobLink1 = "https://www.linkedin.com/link1";
            JobListings existingJobListings = new JobListings(fileName);
            var job1 = new JobListing() { JobLink = jobLink1, ContactInformation = contactInformation1 };
            existingJobListings.InsertOrUpdate(job1);
            var job2 = new JobListing() { JobLink = jobLink1, ContactInformation = updatedContactInformation };
            JobListings newJobListings = new JobListings(fileName);
            newJobListings.InsertOrUpdate(job2);

            // existing and new  with same URL, new is ignored
            var res = SeleniumTestsHelpers.MergeJobListingsIgnoreAlreadyExisting(newJobListings.JobListingsList, existingJobListings.JobListingsList);
            Assert.That(res.Count, Is.EqualTo(1), "The merged list should only contain the 1 job listing.");
            Assert.That(res.FirstOrDefault().ContactInformation, Is.EquivalentTo(contactInformation1), "Contact information from the new JobListing should not be used.");
        }

        [Test]
        public void MergeJobListingsOverWriteAlreadyExisting()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"UpdateExistingJobListings_{randomName}";
            var contactInformation1 = "Original Contact Information 1";
            var contactInformation2 = "Original Contact Information 2";
            var updatedContactInformation = "updated Contact Information";
            var jobLink1 = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            JobListings existingJobListings = new JobListings(fileName);
            var job1 = new JobListing() { JobLink = jobLink1, ContactInformation = contactInformation1 };
            var job2 = new JobListing() { JobLink = jobLink2, ContactInformation = contactInformation2 };
            existingJobListings.InsertOrUpdate(job1);
            existingJobListings.InsertOrUpdate(job2);

            var jobUpdated = new JobListing() { JobLink = jobLink1, ContactInformation = updatedContactInformation };
            JobListings newJobListings = new JobListings(fileName);
            newJobListings.InsertOrUpdate(jobUpdated);

            // existing and new  with same URL, new is overWritten
            var res = SeleniumTestsHelpers.MergeJobListingsOverWriteAlreadyExisting(newJobListings.JobListingsList, existingJobListings.JobListingsList);
            Assert.That(res.Count, Is.EqualTo(2), "The merged list should only contain the 2 job listings.");
            Assert.That(res.FirstOrDefault(e=> e.JobLink == jobLink1).ContactInformation, Is.EquivalentTo(updatedContactInformation), "Contact information from the new JobListing should  be used.");
        }

        [Test]
        public void MergePeopleToCompany()
        {
            // create object programmatically
            var company = new CompanyListing()
            {
                OrgNumber = "123456-7890",
                CompanyName = "Test Company",
                Description = "This is a test company.",
                TurnoverYear = 2023,
                Turnover = 1000000,
                Adress = "Test Address",
                SourceLink = "https://www.testcompany.com"
            };
            var companyListings = new CompanyListings("companyListings");
            companyListings.InsertOrUpdate(company);

            var peopleLinkedInDetail1 = new PeopleLinkedInDetail()
            {
                Name = "John Doe",
                OrgNumber = "123456-7890",
                Title = "CEO at Test Company",
                LinkedInLink = "https://www.linkedin.com/company/1234567890",
                CompanyName = "Test Company",
            };
            var peopleLinkedInDetail2 = new PeopleLinkedInDetail()
            {
                Name = "Jane Doe",
                OrgNumber = "123456-7890",
                Title = "CTO at Test Company",
                LinkedInLink = "https://www.linkedin.com/company/123",
                CompanyName = "Test Company",
            };
            var peopleLinkedInDetails = new PeopleLinkedInDetails("details");
            peopleLinkedInDetails.InsertOrUpdate(peopleLinkedInDetail1);
            peopleLinkedInDetails.InsertOrUpdate(peopleLinkedInDetail2);

            var companyWithPeoples = new CompanyWithPeoples("companyWith people collection");

            foreach (var companyListing in companyListings.CompanyListingsList)
            {
                // find people linked to the company
                var peopleLinkedInDetailsList = peopleLinkedInDetails.PeopleLinkedInDetailsList
                    .Where(p => p.CompanyName == companyListing.CompanyName).ToList();

                var newItem = CompanyMapper.MapToCompanyWithPeople(companyListing,peopleLinkedInDetailsList);
                companyWithPeoples.InsertOrUpdate(newItem);
            }

            // Write to file
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"MergePeopleToCompany{randomName}";
            SeleniumTestsHelpers.WriteToFile(companyWithPeoples, fileName);

            CompanyWithPeoples updatedJobListingsFromFile = SeleniumTestsHelpers.LoadCompanyWithPeoplesFromFile(fileName);
            Assert.That(updatedJobListingsFromFile.CompanyWithPeopleList.Count, Is.EqualTo(1), "The re-loaded CompanyWithPeople from file does not contain the same number of objects.");
        }

        [TestCase("merged_filter_emp_and_turnover_applied.csv", "peopleDetail.csv","mergePeopleToCompany")]
        public void MergePeopleToCompanyExistingFile(string existingCompanyFile, string existingPeopleDetailsFile, string newFileName)
        {
            var companyListings = SeleniumTestsHelpers.LoadCompanyListingsFromFile(existingCompanyFile);
            var peopleLinkedInDetails = SeleniumTestsHelpers.LoadPeoplesFromFile(existingPeopleDetailsFile);
            var companyWithPeoples = new CompanyWithPeoples("companyWith people collection");

            foreach (var companyListing in companyListings.CompanyListingsList)
            {
                // find people linked to the company
                var peopleLinkedInDetailsList = peopleLinkedInDetails.PeopleLinkedInDetailsList
                    .Where(p => p.CompanyName.Trim() == companyListing.CompanyName.Trim()).ToList();

                var newItem = CompanyMapper.MapToCompanyWithPeople(companyListing, peopleLinkedInDetailsList);
                companyWithPeoples.InsertOrUpdate(newItem);
            }

            // Write to file
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"{newFileName}_{randomName}";
            SeleniumTestsHelpers.WriteToFile(companyWithPeoples, fileName);

            CompanyWithPeoples updatedJobListingsFromFile = SeleniumTestsHelpers.LoadCompanyWithPeoplesFromFile(fileName);
            Assert.That(updatedJobListingsFromFile.CompanyWithPeopleList.Count, Is.EqualTo(98), "The re-loaded CompanyWithPeople from file does not contain the same number of objects.");
        }


        /// <summary>
        /// Merge all files into one CSV file.
        /// </summary>
        /// <param name="inputFolder"></param>
        [TestCase("CompanyListings")]
        [TestCase("CompanyListingsVG")]
        [TestCase("CompanyListingsSkane")]
        public void MergeAllCVFilesToOne(string inputFolder)
        {
            string testName = "MergedFilesToOne";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{testName}_{inputFolder}_{timestamp}.csv";
            SeleniumTestsHelpers.MergeAllCVFilesToOne(inputFolder, fileName);
            var filePath = Path.Combine(SeleniumTestsHelpers.GetOutputFolderPath(), fileName);
            Assert.That(File.Exists(filePath), Is.True, $"The merged file {fileName} was not created successfully.");
        }
    }
}
