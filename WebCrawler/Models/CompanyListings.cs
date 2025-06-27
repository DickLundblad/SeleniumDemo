namespace WebCrawler.Models
{
    public class CompanyListings
    {
        private List<CompanyListing> companyListings;
        private string startPage;
        private string name;

        public CompanyListings(string name, string startPage="")
        {
            companyListings = new List<CompanyListing>();
            this.Name = name;
            this.StartPage = startPage;
        }

        public List<CompanyListing> CompanyListingsList
        {
            get { return companyListings; }
            set { companyListings = value; }
        }

        public string StartPage { get => startPage; set => startPage = value; }
        public string Name { get => name; set => name = value; }

        public bool InsertOrUpdate(CompanyListing job)
        {
            var comparer = new CompanyListingComparer();
            var existingCompany = companyListings.FirstOrDefault(j => comparer.Equals(j, job));

            if (existingCompany != null)
            {
                // Update
                existingCompany.Description = job.Description;
                existingCompany.TurnoverYear = job.TurnoverYear;
                existingCompany.Turnover = job.Turnover;
                existingCompany.Adress = job.Adress;
                existingCompany.CompanyName = job.CompanyName;
                existingCompany.SourceLink = job.SourceLink;

                Console.WriteLine($"Company updated: {job.OrgNumber}");
                return true;
            }
            else
            {
                // Insert
                companyListings.Add(job);
                Console.WriteLine($"Company added: {job.OrgNumber}");
                return true;
            }
        }

        public bool InsertIgnoreAlreadyExistingCompany(CompanyListing job)
        {
            var comparer = new CompanyListingComparer();
            var existingCompany = companyListings.FirstOrDefault(j => comparer.Equals(j, job));

            if (existingCompany == null)
            {
                companyListings.Add(job);
                Console.WriteLine($"Company added: {job.OrgNumber}");
                return true;
            }
            else
            {
                Console.WriteLine($"Company: {job.OrgNumber} already existed and will be ignopre, no update performed");
                return true;
            }
        }
    }
}
