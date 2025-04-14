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
            List<JobListing> jobListings = new List<JobListing>();
            var job1 = new JobListing() { JobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433" };
            jobListings.Add(job1);
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
            List<JobListing> jobListings = new List<JobListing>();
            var job1 = new JobListing() { JobLink = jobLink};
            jobListings.Add(job1);

            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);
            List<JobListing> jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);

            Assert.That(jobListingsFromFile.Count, Is.EqualTo(jobListings.Count), "The re-loaded JobListing from file does not contain the same number of objects.");
            Assert.That(jobListingsFromFile[0].JobLink, Is.EqualTo(jobLink), "The re-loaded JobListing from file does not contain the same object.");
            Assert.That(jobListingsFromFile, Is.EquivalentTo(jobListings).Using(new JobListingComparer()), 
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
            List<JobListing> jobListings = new List<JobListing>();
            var job1 = new JobListing() { JobLink = jobLink};
            var job2 = new JobListing() { JobLink = jobLink2};
            jobListings.Add(job1);
            jobListings.Add(job2);
            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);

            List<JobListing> jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            jobListingsFromFile[0].JobLink = updatedJobLink;

            SeleniumTestsHelpers.WriteToFile(jobListingsFromFile, fileName);

            List<JobListing> updatedJobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);

            Assert.That(updatedJobListingsFromFile.Count, Is.EqualTo(jobListings.Count), "The re-loaded JobListing from file does not contain the same number of objects.");
            Assert.That(updatedJobListingsFromFile[0].JobLink, Is.EqualTo(updatedJobLink), "The re-loaded JobListing from file does not contain the same object.");
            Assert.That(updatedJobListingsFromFile, Is.Not.EquivalentTo(jobListings).Using(new JobListingComparer()), 
            "The re-loaded updated JobListings from file should notmatch. the jobLink is used for comparison ");
        }

        // if jobList exist, don't add, just update the ojbect
        [Test]
        public void UpdateExistingJobListings()
       { 
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"UpdateExistingJobListings_{randomName}.tsv";
            var contactInformation1 = "Original Contact Information 1";
            var contactInformation2 = "Original Contact Information 2";
            var updatedContactInformation = "updated Contact Information";
            var jobLink1 = "https://www.linkedin.com/link1";
            var jobLink2 = "https://www.linkedin.com/link2";
            List<JobListing> jobListings = new List<JobListing>();
            var job1 = new JobListing() { JobLink = jobLink1, ContactInformation = contactInformation1};
            var job2 = new JobListing() { JobLink = jobLink2, ContactInformation = contactInformation2};
            jobListings.Add(job1);
            jobListings.Add(job2);
            SeleniumTestsHelpers.WriteToFile(jobListings, fileName);

            List<JobListing> jobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            // find the jobListing with jobLink1 and update it
            var itemToUpdate = jobListingsFromFile.FirstOrDefault(x => x.JobLink == jobLink1);
            itemToUpdate.ContactInformation = updatedContactInformation;
            SeleniumTestsHelpers.WriteToFile(jobListingsFromFile, fileName);

            List<JobListing> updatedJobListingsFromFile = SeleniumTestsHelpers.LoadJobListingsFromFile(fileName);
            var updatedItem = updatedJobListingsFromFile.FirstOrDefault(x => x.JobLink == jobLink1);

            Assert.That(updatedItem, Is.EqualTo(job1).Using(new JobListingComparer()), "The updated item did not have the correct ContactInformation");
            Assert.That(updatedItem.ContactInformation, Is.EqualTo(updatedContactInformation), "The re-loaded JobListing from file does not contain the same object.");
        }
    }
}
