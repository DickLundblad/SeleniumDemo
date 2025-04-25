
using DocumentFormat.OpenXml.Math;

namespace WebCrawler.Models
{
    public class JobListings
    {
        private List<JobListing> jobListings;
        private string startPage;
        private string name;

        public JobListings(string name, string startPage="")
        {
            jobListings = new List<JobListing>();
            this.Name = name;
            this.StartPage = startPage;
        }

        public List<JobListing> JobListingsList
        {
            get { return jobListings; }
            set { jobListings = value; }
        }

        public string StartPage { get => startPage; set => startPage = value; }
        public string Name { get => name; set => name = value; }

        public bool InsertOrUpdate(JobListing job)
        {
            var comparer = new JobListingComparer();
            var existingJob = jobListings.FirstOrDefault(j => comparer.Equals(j, job));

            if (existingJob != null)
            {
                // Update the existing job
                existingJob.Title = job.Title;
                existingJob.Published = job.Published;
                existingJob.EndDate = job.EndDate;
                existingJob.ContactInformation = job.ContactInformation;
                existingJob.Description = job.Description;
                existingJob.ApplyLink = job.ApplyLink;

                Console.WriteLine($"Job updated: {job.JobLink}");
                return true; // Job updated successfully
            }
            else
            {
                // Insert the new job
                jobListings.Add(job);
                Console.WriteLine($"Job added: {job.JobLink}");
                return true; // Job added successfully
            }
        }
    }
}
