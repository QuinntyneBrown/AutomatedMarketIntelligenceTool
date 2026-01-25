namespace Search.Core.Entities;

/// <summary>
/// A saved search profile for recurring searches.
/// </summary>
public sealed class SearchProfile
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public SearchCriteria Criteria { get; private set; } = SearchCriteria.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UserId { get; private set; }

    private SearchProfile() { }

    public static SearchProfile Create(string name, SearchCriteria criteria, Guid? userId = null)
    {
        return new SearchProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            Criteria = criteria,
            IsActive = true,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string? name = null, SearchCriteria? criteria = null)
    {
        if (name != null) Name = name;
        if (criteria != null) Criteria = criteria;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Criteria for searching vehicles.
/// </summary>
public sealed record SearchCriteria
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? PriceFrom { get; init; }
    public decimal? PriceTo { get; init; }
    public int? MileageFrom { get; init; }
    public int? MileageTo { get; init; }
    public string? BodyStyle { get; init; }
    public string? Transmission { get; init; }
    public string? Drivetrain { get; init; }
    public string? FuelType { get; init; }
    public string? ExteriorColor { get; init; }
    public string? Province { get; init; }
    public string? City { get; init; }
    public int? RadiusKm { get; init; }
    public string? Keywords { get; init; }
    public bool? CertifiedPreOwned { get; init; }
    public bool? DealerOnly { get; init; }
    public bool? PrivateSellerOnly { get; init; }

    public static SearchCriteria Empty => new();
}
