﻿namespace WebCrawler.Models
{
    public class CompanyWithPeople : CompanyListing
    {
        public bool Refresh { get; set; } = false;
        public string ContactName1 { get; set; }
        public string ContactRole1 { get; set; }
        public string ContactTitle1 { get; set; }
        public string ContactEmail1 { get; set; }
        public string ContactLinkedIn1 { get; set; }
        public string ContactName2 { get; set; }
        public string ContactRole2 { get; set; }
        public string ContactTitle2 { get; set; }
        public string ContactEmail2 { get; set; }
        public string ContactLinkedIn2 { get; set; }
        public string ContactName3 { get; set; }
        public string ContactRole3 { get; set; }
        public string ContactTitle3 { get; set; }
        public string ContactEmail3 { get; set; }
        public string ContactLinkedIn3 { get; set; }
        public string ExtraContacts { get; set; }
    }
}
