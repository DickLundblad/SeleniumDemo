using WebCrawler.Models;

namespace WebCrawler
{
    [TestFixture]
    public class PeopleLinkedInDetailsTests
    {

        [Test]
        public void ValidateThatDuplicatesCantBeAddedToCollection()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"ValidateThatDuplicatesCantBeAddedToCollection{randomName}";
            var linkedinLink1 = "https://www.linkedin.com/in/dick-lundblad/";
            var linkedinLink2 = "https://www.linkedin.com/in/david-dormvik-a6604790/";
            PeopleLinkedInDetails people = new PeopleLinkedInDetails(fileName);

            var comp1 = new PeopleLinkedInDetail() { LinkedInLink = linkedinLink1 };
            var compDuplicate = new PeopleLinkedInDetail() { LinkedInLink = linkedinLink1 };
            var comp2 = new PeopleLinkedInDetail() { LinkedInLink = linkedinLink2 };

            people.InsertOrUpdate(comp1);
            people.InsertOrUpdate(compDuplicate);
            people.InsertOrUpdate(comp2);

            Assert.That(people.PeopleLinkedInDetailsList.Count, Is.EqualTo(2), "The collection should only contain one item after inserting two(2) PeopleLinkedInDetails and one(1) duplicate.");
        }

        [Test]
        public void ValidateThatDuplicatesCantBeAddedToCollection_WriteToFile()
        {
            var randomName = Guid.NewGuid().ToString(); // Generate a random name
            var fileName = $"ValidateThatDuplicatesCantBeAddedToCollection_WriteToFile{randomName}";
            var linkedinLink1 = "https://www.linkedin.com/in/dick-lundblad/";
            var linkedinLink2 = "https://www.linkedin.com/in/david-dormvik-a6604790/";
            PeopleLinkedInDetails people = new PeopleLinkedInDetails(fileName);

            var comp1 = new PeopleLinkedInDetail() { LinkedInLink = linkedinLink1 };
            var compDuplicate = new PeopleLinkedInDetail() { LinkedInLink = linkedinLink1 };
            var comp2 = new PeopleLinkedInDetail() { LinkedInLink = linkedinLink2 };

            people.InsertOrUpdate(comp1);
            people.InsertOrUpdate(compDuplicate);
            people.InsertOrUpdate(comp2);

           // SeleniumTestsHelpers.WriteToFile(people, fileName);
            var findFileInfo = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{fileName}.*");
            Assert.That(findFileInfo.Length, Is.GreaterThan(0), "The file was not created.");
        }
    }
}
