namespace WebCrawler.Models
{
    internal class CompanyWithPeoplesComparer : IEqualityComparer<CompanyWithPeoples>
    {
        public bool Equals(CompanyWithPeoples x, CompanyWithPeoples y)
        {
            return x?.Name == y?.Name;
        }

        public int GetHashCode(CompanyWithPeoples obj)
        {
            return obj.Name?.GetHashCode() ?? 0;
        }
    }
}