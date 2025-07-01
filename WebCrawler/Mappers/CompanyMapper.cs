using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCrawler.Models;

namespace WebCrawler.Mappers
{
    public static class CompanyMapper
    {
        public static CompanyWithPeople MapToCompanyWithPeople(CompanyListing companyListing)
        {
            if (companyListing == null)
            {
                throw new ArgumentNullException(nameof(companyListing), "CompanyListing cannot be null.");
            }
            return new CompanyWithPeople
            {
                OrgNumber = companyListing.OrgNumber,
                CompanyName = companyListing.CompanyName,
                SourceLink = companyListing.SourceLink,
                Description = companyListing.Description,
                TurnoverYear = companyListing.TurnoverYear,
                Turnover = companyListing.Turnover,
                Adress = companyListing.Adress,
                NumberOfEmployes = companyListing.NumberOfEmployes,
                Refresh = false, // Default value, can be set later
            };
        }

        public static CompanyWithPeople MapToCompanyWithPeople(CompanyListing companyListing, List<PeopleLinkedInDetail> listOfPeople)
        {
            var companyWithPeople = MapToCompanyWithPeople(companyListing);

            if (listOfPeople.Count > 0)
            {
                companyWithPeople.ContactEmail1 = listOfPeople[0].Email;
                companyWithPeople.ContactLinkedIn1 = listOfPeople[0].LinkedInLink;
                companyWithPeople.ContactTitle1 = listOfPeople[0].Title;
                companyWithPeople.ContactName1 = listOfPeople[0].Name;
                companyWithPeople.ContactRole1 = listOfPeople[0].Role;

                if (listOfPeople.Count > 1)
                {
                    companyWithPeople.ContactEmail2 = listOfPeople[1].Email;
                    companyWithPeople.ContactLinkedIn2 = listOfPeople[1].LinkedInLink;
                    companyWithPeople.ContactTitle2 = listOfPeople[1].Title;
                    companyWithPeople.ContactName2 = listOfPeople[1].Name;
                    companyWithPeople.ContactRole2 = listOfPeople[1].Role;

                }
                if (listOfPeople.Count > 2)
                {
                    // If there are more than 2 people, we can handle them as needed.
                    // For now, we will just ignore them.
                    // You can extend this logic to handle more contacts if required.
                    System.Console.WriteLine($"More than 2 contacts found for {companyListing.CompanyName}. Only the first two will be mapped.");
                }
            }
            
            return companyWithPeople;
        }
    }
}
