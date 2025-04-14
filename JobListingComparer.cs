using SeleniumDemo.Models;

public class JobListingComparer : IEqualityComparer<JobListing>
{
    public bool Equals(JobListing x, JobListing y)
    {
        return x?.JobLink == y?.JobLink;
    }

    public int GetHashCode(JobListing obj)
    {
        return obj.JobLink?.GetHashCode() ?? 0;
    }
}
