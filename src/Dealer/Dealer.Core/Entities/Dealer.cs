namespace Dealer.Core.Entities;

/// <summary>
/// Entity representing an automotive dealer.
/// </summary>
public sealed class Dealer
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Website { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public decimal ReliabilityScore { get; private set; }
    public int ListingCount { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Dealer() { }

    public static Dealer Create(
        string name,
        string? website = null,
        string? phone = null,
        string? address = null,
        string? city = null,
        string? province = null,
        string? postalCode = null)
    {
        return new Dealer
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = NormalizeName(name),
            Website = website,
            Phone = phone,
            Address = address,
            City = city,
            Province = province,
            PostalCode = postalCode,
            ReliabilityScore = 100.0m,
            ListingCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(
        string? name = null,
        string? website = null,
        string? phone = null,
        string? address = null,
        string? city = null,
        string? province = null,
        string? postalCode = null)
    {
        if (name != null)
        {
            Name = name;
            NormalizedName = NormalizeName(name);
        }
        if (website != null) Website = website;
        if (phone != null) Phone = phone;
        if (address != null) Address = address;
        if (city != null) City = city;
        if (province != null) Province = province;
        if (postalCode != null) PostalCode = postalCode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateReliabilityScore(decimal newScore)
    {
        ReliabilityScore = Math.Clamp(newScore, 0m, 100m);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncrementListingCount()
    {
        ListingCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DecrementListingCount()
    {
        if (ListingCount > 0)
        {
            ListingCount--;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void SetListingCount(int count)
    {
        ListingCount = Math.Max(0, count);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string NormalizeName(string name)
    {
        return name.ToUpperInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Trim();
    }
}
