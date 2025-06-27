using WebCrawler.Models;

public class PeopleLinkedInDetailComparer: IEqualityComparer<PeopleLinkedInDetail>
{
    public bool Equals(PeopleLinkedInDetail x, PeopleLinkedInDetail y)
    {
        return x?.LinkedInLink == y?.LinkedInLink;
    }

    public int GetHashCode(PeopleLinkedInDetail obj)
    {
        return obj.LinkedInLink?.GetHashCode() ?? 0;
    }
}
