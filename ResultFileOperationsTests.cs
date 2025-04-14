using SeleniumDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var jobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433";
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
            var jobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433";
            var updatedJobLink = "https://www.linkedin.com/jobs/updatedlink";
            List<JobListing> jobListings = new List<JobListing>();
            var job1 = new JobListing() { JobLink = jobLink};
            jobListings.Add(job1);
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

        // update objects in file  CRUD(Write, Read, update, Write, read) 

        // if jobList exist, don't add, just update the ojbect
    }
}
