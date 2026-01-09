namespace AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;

public class SearchProfileId
{
    public Guid Value { get; private set; }

    private SearchProfileId(Guid value)
    {
        Value = value;
    }

    public static SearchProfileId Create()
    {
        return new SearchProfileId(Guid.NewGuid());
    }

    public static SearchProfileId From(Guid value)
    {
        return new SearchProfileId(value);
    }

    public override bool Equals(object? obj)
    {
        return obj is SearchProfileId other && Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static implicit operator Guid(SearchProfileId id) => id.Value;
}
