namespace WebCrawler.Models
{
    public class  CompanyLinkedInDetails
    {
        public bool Refresh { get; set; } = false;

        // OrgNumber foreign key to CompanyListing
        public string OrgNumber { get; set; } = string.Empty;

        // CompanyName redundant, but kept for reding
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLinkedInPage { get; set; } = string.Empty;
    }
}
