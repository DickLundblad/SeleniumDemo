namespace WebCrawler.Models
{
    internal class PeopleLinkedInDetailsComparer:IEqualityComparer<PeopleLinkedInDetails>
    {
        public bool Equals(PeopleLinkedInDetails x, PeopleLinkedInDetails y)
        {
            return x?.Name == y?.Name;
        }

        public int GetHashCode(PeopleLinkedInDetails obj)
        {
            return obj.Name?.GetHashCode() ?? 0;
        }
    }
}