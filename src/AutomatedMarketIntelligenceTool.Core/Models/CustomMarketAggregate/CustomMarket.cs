namespace AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;

/// <summary>
/// Represents a custom market region definition for targeted searches.
/// </summary>
public class CustomMarket
{
    public CustomMarketId CustomMarketId { get; private set; } = null!;
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The name of the custom market (e.g., "Greater Toronto Area", "Pacific Northwest").
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional description of the market region.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Comma-separated list of postal codes or regions included in this market.
    /// </summary>
    public string PostalCodes { get; private set; } = null!;

    /// <summary>
    /// Comma-separated list of provinces/states included in this market.
    /// </summary>
    public string? Provinces { get; private set; }

    /// <summary>
    /// The center latitude for distance calculations.
    /// </summary>
    public double? CenterLatitude { get; private set; }

    /// <summary>
    /// The center longitude for distance calculations.
    /// </summary>
    public double? CenterLongitude { get; private set; }

    /// <summary>
    /// Radius in kilometers from center point.
    /// </summary>
    public int? RadiusKm { get; private set; }

    /// <summary>
    /// Whether this market is active and can be used for searches.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Priority for ordering (lower = higher priority).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// JSON configuration for market-specific settings (scraper preferences, etc.).
    /// </summary>
    public string? ConfigurationJson { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private CustomMarket() { }

    public static CustomMarket Create(
        Guid tenantId,
        string name,
        string postalCodes,
        string? description = null,
        string? provinces = null,
        double? centerLatitude = null,
        double? centerLongitude = null,
        int? radiusKm = null,
        int priority = 100,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Market name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(postalCodes))
            throw new ArgumentException("Postal codes cannot be empty", nameof(postalCodes));

        return new CustomMarket
        {
            CustomMarketId = CustomMarketId.CreateNew(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            PostalCodes = postalCodes.Trim(),
            Provinces = provinces?.Trim(),
            CenterLatitude = centerLatitude,
            CenterLongitude = centerLongitude,
            RadiusKm = radiusKm,
            IsActive = true,
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string postalCodes,
        string? description = null,
        string? provinces = null,
        double? centerLatitude = null,
        double? centerLongitude = null,
        int? radiusKm = null,
        int? priority = null,
        string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Market name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(postalCodes))
            throw new ArgumentException("Postal codes cannot be empty", nameof(postalCodes));

        Name = name.Trim();
        PostalCodes = postalCodes.Trim();
        Description = description?.Trim();
        Provinces = provinces?.Trim();
        CenterLatitude = centerLatitude;
        CenterLongitude = centerLongitude;
        RadiusKm = radiusKm;

        if (priority.HasValue)
            Priority = priority.Value;

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetConfiguration(string? configurationJson, string? updatedBy = null)
    {
        ConfigurationJson = configurationJson;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Activate(string? updatedBy = null)
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(string? updatedBy = null)
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetPriority(int priority, string? updatedBy = null)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public IReadOnlyList<string> GetPostalCodeList()
    {
        return PostalCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public IReadOnlyList<string> GetProvinceList()
    {
        if (string.IsNullOrWhiteSpace(Provinces))
            return Array.Empty<string>();

        return Provinces.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
