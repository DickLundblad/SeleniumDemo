using WebCrawler.Models;

public class CompanyWithPeopleComparer : IEqualityComparer<CompanyWithPeople>
{
    public bool Equals(CompanyWithPeople x, CompanyWithPeople y)
    {
        return x?.OrgNumber == y?.OrgNumber && x?.ContactLinkedIn1 == y?.ContactLinkedIn1 && x?.ContactLinkedIn2 == y?.ContactLinkedIn2;
    }

    public int GetHashCode(CompanyWithPeople obj)
    {
        return obj.OrgNumber?.GetHashCode() ?? 0;
    }
}
