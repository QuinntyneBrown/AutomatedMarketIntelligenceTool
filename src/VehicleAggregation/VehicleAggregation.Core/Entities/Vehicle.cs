namespace VehicleAggregation.Core.Entities;

/// <summary>
/// Aggregated vehicle representing multiple listings of the same vehicle.
/// </summary>
public sealed class Vehicle
{
    public Guid Id { get; init; }
    public string? VIN { get; private set; }
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string? Trim { get; private set; }
    public string? BodyStyle { get; private set; }
    public string? Transmission { get; private set; }
    public string? Drivetrain { get; private set; }
    public string? FuelType { get; private set; }
    public string? ExteriorColor { get; private set; }
    public string? InteriorColor { get; private set; }

    public decimal? BestPrice { get; private set; }
    public decimal? AveragePrice { get; private set; }
    public decimal? LowestPrice { get; private set; }
    public decimal? HighestPrice { get; private set; }
    public int ListingCount { get; private set; }

    public Guid? BestPriceListingId { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<VehicleListing> _listings = [];
    public IReadOnlyList<VehicleListing> Listings => _listings.AsReadOnly();

    private Vehicle() { }

    public static Vehicle Create(
        string make,
        string model,
        int year,
        string? vin = null,
        string? trim = null)
    {
        return new Vehicle
        {
            Id = Guid.NewGuid(),
            Make = make,
            Model = model,
            Year = year,
            VIN = vin,
            Trim = trim,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddListing(Guid listingId, decimal price, string source, string? dealerName)
    {
        var existing = _listings.FirstOrDefault(l => l.ListingId == listingId);
        if (existing != null)
        {
            existing.UpdatePrice(price);
        }
        else
        {
            _listings.Add(VehicleListing.Create(listingId, price, source, dealerName));
        }
        RecalculatePrices();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveListing(Guid listingId)
    {
        var listing = _listings.FirstOrDefault(l => l.ListingId == listingId);
        if (listing != null)
        {
            _listings.Remove(listing);
            RecalculatePrices();
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateDetails(
        string? trim = null,
        string? bodyStyle = null,
        string? transmission = null,
        string? drivetrain = null,
        string? fuelType = null,
        string? exteriorColor = null,
        string? interiorColor = null)
    {
        if (trim != null) Trim = trim;
        if (bodyStyle != null) BodyStyle = bodyStyle;
        if (transmission != null) Transmission = transmission;
        if (drivetrain != null) Drivetrain = drivetrain;
        if (fuelType != null) FuelType = fuelType;
        if (exteriorColor != null) ExteriorColor = exteriorColor;
        if (interiorColor != null) InteriorColor = interiorColor;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculatePrices()
    {
        if (_listings.Count == 0)
        {
            BestPrice = null;
            AveragePrice = null;
            LowestPrice = null;
            HighestPrice = null;
            BestPriceListingId = null;
            ListingCount = 0;
            return;
        }

        var prices = _listings.Select(l => l.Price).ToList();
        LowestPrice = prices.Min();
        HighestPrice = prices.Max();
        AveragePrice = prices.Average();
        BestPrice = LowestPrice;
        ListingCount = _listings.Count;

        var bestListing = _listings.OrderBy(l => l.Price).First();
        BestPriceListingId = bestListing.ListingId;
    }
}

/// <summary>
/// A listing associated with an aggregated vehicle.
/// </summary>
public sealed class VehicleListing
{
    public Guid Id { get; init; }
    public Guid ListingId { get; init; }
    public decimal Price { get; private set; }
    public string Source { get; init; } = string.Empty;
    public string? DealerName { get; init; }
    public DateTimeOffset AddedAt { get; init; }
    public DateTimeOffset? PriceUpdatedAt { get; private set; }

    private VehicleListing() { }

    public static VehicleListing Create(Guid listingId, decimal price, string source, string? dealerName)
    {
        return new VehicleListing
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            Price = price,
            Source = source,
            DealerName = dealerName,
            AddedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (Price != newPrice)
        {
            Price = newPrice;
            PriceUpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
