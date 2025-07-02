namespace WebCrawler.Models
{
    internal class CompanyLinkedInDetailsComparer:IEqualityComparer<CompanyLinkedInDetails>
    {
        public bool Equals(CompanyLinkedInDetails x, CompanyLinkedInDetails y)
        {
            return x?.Name == y?.Name;
        }

        public int GetHashCode(CompanyLinkedInDetails obj)
        {
            return obj.Name?.GetHashCode() ?? 0;
        }
    }
}