namespace WebCrawler.Models
{

    public class CompanyLinkedInDetails
    {
        private List<CompanyLinkedInDetail> companyListings;
        private string name;

        public CompanyLinkedInDetails(string name, string startPage = "")
        {
            companyListings = new List<CompanyLinkedInDetail>();
            this.Name = name;

        }

        public List<CompanyLinkedInDetail> CompanyListingsList
        {
            get { return companyListings; }
            set { companyListings = value; }
        }

        public string Name { get => name; set => name = value; }

        public bool InsertOrUpdate(CompanyLinkedInDetail job)
        {
            var comparer = new CompanyLinkedInDetailComparer();
            var existingCompany = companyListings.FirstOrDefault(j => comparer.Equals(j, job));

            if (existingCompany != null)
            {
                // Update
                existingCompany.LinkedInLink = job.LinkedInLink;
                existingCompany.CompanyWebsite = job.CompanyWebsite;
                existingCompany.CompanyName = job.CompanyName;

                Console.WriteLine($"CompanyLinkedInDetail updated:  {job.CompanyName} :  {job.CompanyWebsite}");
                return true;
            }
            else
            {
                // Insert
                companyListings.Add(job);
                Console.WriteLine($"CompanyWebsite added:   {job.CompanyName} :  {job.CompanyWebsite}");
                return true;
            }
        }

        public bool InsertIgnoreAlreadyExistingCompany(CompanyLinkedInDetail job)
        {
            var comparer = new CompanyLinkedInDetailComparer();
            var existingCompany = companyListings.FirstOrDefault(j => comparer.Equals(j, job));

            if (existingCompany == null)
            {
                companyListings.Add(job);
                Console.WriteLine($"CompanyLinkedInDetail added:  {job.CompanyName} :  {job.CompanyWebsite}");
                return true;
            }
            else
            {
                Console.WriteLine($"CompanyLinkedInDetail: {job.CompanyName} : {job.CompanyWebsite} already existed and will be ignopre, no update performed");
                return true;
            }
        }
    }
}