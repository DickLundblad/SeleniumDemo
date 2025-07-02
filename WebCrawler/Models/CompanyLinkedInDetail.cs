namespace WebCrawler.Models
{
    public class CompanyLinkedInDetail
    {
        public bool Refresh { get; set; } = false;

        /// <summary>
        /// key
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        public string LinkedInLink { get; set; } = string.Empty;

        public string CompanyWebsite { get; set; } = string.Empty;
    }
}
