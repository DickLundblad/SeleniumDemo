namespace WebCrawler.Models
{
    public class CompanyListing
    {
        public bool Refresh { get; set; } = false;

        public string CompanyName { get; set; } = string.Empty;
        public string SourceLink { get; set; } = string.Empty;
        public int Turnover { get; set; } = 0;
        public string TurnoverYear { get; set; } = string.Empty;
        public int NumberOfEmployes { get; set; } = 0;
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
