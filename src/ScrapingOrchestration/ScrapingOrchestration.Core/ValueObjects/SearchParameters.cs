namespace ScrapingOrchestration.Core.ValueObjects;

/// <summary>
/// Parameters for a search session.
/// </summary>
public sealed record SearchParameters
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MaxMileage { get; init; }
    public string? PostalCode { get; init; }
    public int? RadiusKm { get; init; }
    public string? Province { get; init; }
    public string? BodyStyle { get; init; }
    public string? Transmission { get; init; }
    public string? FuelType { get; init; }
    public string? Drivetrain { get; init; }
    public string? Condition { get; init; }
    public string? SellerType { get; init; }
    public int MaxResults { get; init; } = 100;

    /// <summary>
    /// Validates the search parameters.
    /// </summary>
    public bool IsValid()
    {
        if (YearFrom.HasValue && YearTo.HasValue && YearFrom > YearTo)
            return false;

        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            return false;

        if (MaxResults <= 0 || MaxResults > 10000)
            return false;

        return true;
    }
}
