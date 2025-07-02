using WebCrawler.Models;

public class CompanyLinkedInDetailComparer : IEqualityComparer<CompanyLinkedInDetail>
    {
        public bool Equals(CompanyLinkedInDetail x, CompanyLinkedInDetail y)
        {
            return x?.CompanyName == y?.CompanyName;
        }

        public int GetHashCode(CompanyLinkedInDetail obj)
        {
            return obj.CompanyName?.GetHashCode() ?? 0;
        }
    }
