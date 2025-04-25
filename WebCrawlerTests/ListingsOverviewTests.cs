using WebCrawler.Models;

namespace WebCrawler.WebCrawlerTests
{
    public class ListingsOverviewTests
    {
        [Test]
        public void CreateListingsOverviewAsExcel()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileNameOverview = TruncateFileToExcelTabLimit($"CreateOverview_{randomName}");
            var fileNameJobListing1 = TruncateFileToExcelTabLimit("CreateJobListings1_{randomName}");
            var fileNameJobListing2 = TruncateFileToExcelTabLimit($"CreateJobListings2_{randomName}");

            JobListings jobListings1 = new JobListings(fileNameJobListing1);
            var job1 = new JobListing() { JobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433" };
            jobListings1.InsertOrUpdate(job1);
            JobListings jobListings2 = new JobListings(fileNameJobListing2);
            var job2 = new JobListing() { JobLink = "https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest" };
            jobListings2.InsertOrUpdate(job2);

            
            ListingsOverview overView = new ListingsOverview();
            overView.InsertOrUpdate(jobListings1);
            overView.InsertOrUpdate(jobListings2);
            SeleniumTestsHelpers.WriteToExcel(overView, fileNameOverview);

            var findFileInfo = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{fileNameOverview}.*");
            Assert.That(findFileInfo.Length, Is.GreaterThan(0), "The file was not created.");
        }

        [Test] 
        public void ReadListingsOverviewFromExcel()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileNameOverview = $"CreateOverview_{randomName}";
            var fileNameJobListing1 = TruncateFileToExcelTabLimit($"CreateJobListings1_{randomName}");
            var fileNameJobListing2 = TruncateFileToExcelTabLimit($"CreateJobListings2_{randomName}");
            JobListings jobListings1 = new JobListings(fileNameJobListing1);
            var job1 = new JobListing() { JobLink = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433" };
            jobListings1.InsertOrUpdate(job1);
            JobListings jobListings2 = new JobListings(fileNameJobListing2);
            var job2 = new JobListing() { JobLink = "https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newest", ContactInformation = "some information" };
            jobListings2.InsertOrUpdate(job2);

            ListingsOverview overView = new ListingsOverview();
            overView.InsertOrUpdate(jobListings1);
            overView.InsertOrUpdate(jobListings2);
            SeleniumTestsHelpers.WriteToExcel(overView, fileNameOverview);

            ListingsOverview listingsOverviewFromFile = GetListingsOverviewFromFile(fileNameOverview);
            Assert.That(listingsOverviewFromFile.JobListings.Count, Is.EqualTo(overView.JobListings.Count), "The re-loaded ListingsOverview from file does not contain the same number of objects.");
            Assert.That(listingsOverviewFromFile.JobListings[0].JobListingsList[0].JobLink, Is.EqualTo(job1.JobLink), "The re-loaded ListingsOverview from file does not contain the same JobLink .");
            Assert.That(listingsOverviewFromFile.JobListings[1].JobListingsList[0].ContactInformation, Is.EqualTo(job2.ContactInformation), "The re-loaded ListingsOverview from file does not contain the same ContactInformation.");
        }

        /// <summary>
        /// Test to check if the existing overview is updated with new job listings
        /// JobLink are considered to be primary key, but only unique in a list with the same name
        /// </summary>
        [Test] 
        public void InsertOrUpdateExistingOverview()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileNameOverview = $"CreateOverview_{randomName}";
            var nameJobListing1 = TruncateFileToExcelTabLimit($"CreateJobListings1_{randomName}");
            var nameJobListing2 = TruncateFileToExcelTabLimit($"CreateJobListings2_{randomName}");
            var jobLink1 = "https://www.linkedin.com/jobs/collections/it-services-and-it-consulting/?currentJobId=4170820433";
            var jobLink2 = "https://jobbsafari.se/lediga-jobb/kategori/data-och-it?sort_by=newes";

            JobListings jobListings1 = new JobListings(nameJobListing1);
            var job1 = new JobListing() { JobLink = jobLink1 };
            jobListings1.InsertOrUpdate(job1);
            JobListings jobListings2 = new JobListings(nameJobListing2);
            var job2 = new JobListing() { JobLink = jobLink2, ContactInformation = "some information" };
            jobListings2.InsertOrUpdate(job2);

            ListingsOverview overView = new ListingsOverview();
            overView.InsertOrUpdate(jobListings1);
            overView.InsertOrUpdate(jobListings2);

            // create a job listing with the same jobLink as job1 but different contact information
            
            var updatedContactInformation = "Some more detailed ContactInformation";
            JobListings jobListingUpdated = new JobListings(nameJobListing1);
            var job1Updated = new JobListing() { JobLink = jobLink1, ContactInformation = updatedContactInformation }; 
            jobListingUpdated.InsertOrUpdate(job1Updated);

            Assert.That(overView.JobListings.Count, Is.EqualTo(2),"Two Joblists should exist before the update");
            overView.InsertOrUpdate(jobListingUpdated);
            var job1UpdatedSeach = overView.JobListings[0].JobListingsList.Where(e => e.JobLink == jobLink1).FirstOrDefault();
            Assert.That(job1UpdatedSeach.ContactInformation, Is.EqualTo(updatedContactInformation), "The re-loaded ListingsOverview from file does not contain the same ContactInformation.");
            Assert.That(overView.JobListings.Count, Is.EqualTo(2),"Since the JoblistIng should on ly be updated the count should be the same");
        }

        private static string TruncateFileToExcelTabLimit(string fileNameJobListing1)
        {
            if (fileNameJobListing1.Length > 31)
            {
                fileNameJobListing1 = fileNameJobListing1.Substring(0, 31);
            }

            return fileNameJobListing1;
        }

        private static ListingsOverview GetListingsOverviewFromFile(string fileNameOverview)
        {
            if (!fileNameOverview.EndsWith(".xlsx"))
            {
                fileNameOverview += ".xlsx";
            }
            return SeleniumTestsHelpers.LoadListingsOverviewFromExcelFile(fileNameOverview);
        }



    }
}
