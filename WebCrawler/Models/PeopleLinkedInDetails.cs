namespace WebCrawler.Models
{
    public class PeopleLinkedInDetails
    {
        private List<PeopleLinkedInDetail> companyListings;
        private string name;

        public PeopleLinkedInDetails(string name)
        {
            companyListings = new List<PeopleLinkedInDetail>();
            this.Name = name;
        }

        public List<PeopleLinkedInDetail> PeopleLinkedInDetailsList
        {
            get { return companyListings; }
            set { companyListings = value; }
        }

        public string Name { get => name; set => name = value; }

        public bool InsertOrUpdate(PeopleLinkedInDetail job)
        {
            var comparer = new PeopleLinkedInDetailComparer();
            var existingCompany = companyListings.FirstOrDefault(j => comparer.Equals(j, job));

            if (existingCompany != null)
            {
                // Update
                /* existingCompany.Description = job.Description;
                 existingCompany.TurnoverYear = job.TurnoverYear;
                 existingCompany.Turnover = job.Turnover;
                 existingCompany.Adress = job.Adress;
                 existingCompany.CompanyName = job.CompanyName;
                 existingCompany.SourceLink = job.SourceLink;

                 Console.WriteLine($"PeopleLinkedInDetails updated: {job.OrgNumber}");*/
                return true;
            }
            else
            {
                // Insert
                companyListings.Add(job);
                Console.WriteLine($"PeopleLinkedInDetails added: {job.OrgNumber}");
                return true;
            }
        }
    }
}
