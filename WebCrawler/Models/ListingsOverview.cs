
namespace SeleniumDemo.Models
{
    public class ListingsOverview
    {
        private List<JobListings> jobListings;

        public ListingsOverview()
        {
            jobListings = new List<JobListings>();
        }

        public bool InsertOrUpdate(JobListings list)
        {
            var comparer = new JobListingsComparer();
            var existingList = jobListings.FirstOrDefault(j => comparer.Equals(j, list));
            if (existingList != null)
            {
                // Update the existing list items
                bool updated = false;
                foreach (var jobListItem in list.JobListingsList)
                {
                    updated = existingList.InsertOrUpdate(jobListItem);
                    if (updated == false)
                    {
                        Console.WriteLine($"Job listings not updated: {jobListItem.JobLink}");
                        return false; // Job listings not updated
                    }
                }
                return true; // Job listings updated successfully
            }
            else
            {
                // Insert the new list
                jobListings.Add(list);
                Console.WriteLine($"Job listings added: {list.Name}");
                return true; // Job listings added successfully
            }
        }
        public List<JobListings> JobListings { get => jobListings; set => jobListings = value; }
    }
}
