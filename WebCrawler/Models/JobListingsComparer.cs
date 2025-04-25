namespace WebCrawler.Models
{
    internal class JobListingsComparer:IEqualityComparer<JobListings>
    {
        public bool Equals(JobListings x, JobListings y)
        {
            return x?.Name == y?.Name;
        }

        public int GetHashCode(JobListings obj)
        {
            return obj.Name?.GetHashCode() ?? 0;
        }
    }
}