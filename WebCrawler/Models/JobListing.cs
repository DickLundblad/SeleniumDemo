﻿namespace WebCrawler.Models
{
    public class JobListing
    {
        public bool Refresh { get; set; } = false;
        public string Title { get; set; } = string.Empty;
        public string JobLink { get; set; } = string.Empty;
        public string Published { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string ContactInformation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApplyLink { get; set; } = string.Empty;
    }
}
