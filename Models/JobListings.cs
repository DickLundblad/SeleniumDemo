
namespace SeleniumDemo.Models
{
    public class JobListings
    {
        private List<JobListing> jobListings;

        public JobListings()
        {
            jobListings = new List<JobListing>();
        }

        public List<JobListing> JobListingsList
        {
            get { return jobListings; }
            set { jobListings = value; }
        }

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

                TestContext.WriteLine($"Job updated: {job.JobLink}");
                return true; // Job updated successfully
            }
            else
            {
                // Insert the new job
                jobListings.Add(job);
                TestContext.WriteLine($"Job added: {job.JobLink}");
                return true; // Job added successfully
            }
        }
    }
}
