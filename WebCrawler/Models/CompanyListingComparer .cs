using WebCrawler.Models;

public class CompanyListingComparer : IEqualityComparer<CompanyListing>
{
    public bool Equals(CompanyListing x, CompanyListing y)
    {
        return x?.OrgNumber == y?.OrgNumber;
    }

    public int GetHashCode(CompanyListing obj)
    {
        return obj.OrgNumber?.GetHashCode() ?? 0;
    }
}
