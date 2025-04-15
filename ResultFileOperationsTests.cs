using SeleniumDemo.Models;

namespace SeleniumDemo
{
    public class ResultFileOperationsTests
    {
        // write objects to file (Write)
        [Test]
        public void CreateJobListings()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"CreateJobListings_{randomName}.tsv";
            JobListings jobListings = new JobListings();
            var job1 = new JobListing() { JobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433" };
            jobListings.InsertOrUpdate(job1);
            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);
            var findFileInfo = Directory.GetFiles(Directory.GetCurrentDirectory(), fileName);
            Assert.That(findFileInfo.Length, Is.GreaterThan(0), "The file was not created.");
        }

        [Test]
        public void ReadJobListings()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"ReadJobListings_{randomName}.tsv";
            var jobLink = "https://www.linkedin.com/link1";
            JobListings jobListings = new JobListings();
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
        public void UpdateJobListings()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"UpdateJobListings_{randomName}.tsv";
            var jobLink = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            var updatedJobLink = "https://www.linkedin.com/updatedlink";
            JobListings jobListings = new JobListings();
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
            var fileName = $"UpdateExistingJobListings_{randomName}.tsv";
            var contactInformation1 = "Original Contact Information 1";
            var contactInformation2 = "Original Contact Information 2";
            var updatedContactInformation = "updated Contact Information";
            var jobLink1 = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            JobListings jobListings = new JobListings();
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
            Assert.That(updatedItem.ContactInformation, Is.EqualTo(updatedContactInformation), "The updated item did not have the correct ContactInformation");
        }
    }
}
