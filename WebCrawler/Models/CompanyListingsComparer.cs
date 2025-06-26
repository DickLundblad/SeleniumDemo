namespace WebCrawler.Models
{
    internal class CompanyListingsComparer:IEqualityComparer<CompanyListings>
    {
        public bool Equals(CompanyListings x, CompanyListings y)
        {
            return x?.Name == y?.Name;
        }

        public int GetHashCode(CompanyListings obj)
        {
            return obj.Name?.GetHashCode() ?? 0;
        }
    }
}