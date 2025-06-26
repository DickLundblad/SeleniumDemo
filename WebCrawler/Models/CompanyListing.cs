namespace WebCrawler.Models
{
    public class CompanyListing
    {
        public bool Refresh { get; set; } = false;

        public string CompanyName { get; set; } = string.Empty;
        public string SourceLink { get; set; } = string.Empty;
        public string Turnover { get; set; } = string.Empty;
        public string TurnoverYear { get; set; } = string.Empty;
        public string NumberOfEmployes { get; set; } = string.Empty;
        public string Adress { get; set; } = string.Empty;
        //VistingAdress
        //VistingAdressZipCode
        //VisitingAdressCity
        public string OrgNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        //ContactName
        //ContactTitle
        //ContactEmail
        //ContactLinkedIn
    }
}
