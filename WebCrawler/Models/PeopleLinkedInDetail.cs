namespace WebCrawler.Models
{
    public class PeopleLinkedInDetail
    {
        public bool Refresh { get; set; } = false;

        /// <summary>
        /// Redundant just for readability
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to CompanyLinkedInDetails and also to CompanyListing
        /// </summary>
        public string OrgNumber { get; set; } = string.Empty;
        public string LinkedInLink { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

    }
}
