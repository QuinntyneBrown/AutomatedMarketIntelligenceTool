using Shared.Contracts.Enums;

namespace Listing.Core.Entities;

/// <summary>
/// Represents a vehicle listing aggregate.
/// </summary>
public sealed class Listing
{
    public Guid Id { get; init; }
    public string SourceListingId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public decimal? Price { get; private set; }
    public string? Make { get; private set; }
    public string? Model { get; private set; }
    public int? Year { get; private set; }
    public int? Mileage { get; private set; }
    public string? VIN { get; private set; }
    public Condition? Condition { get; private set; }
    public BodyStyle? BodyStyle { get; private set; }
    public Transmission? Transmission { get; private set; }
    public FuelType? FuelType { get; private set; }
    public Drivetrain? Drivetrain { get; private set; }
    public string? ExteriorColor { get; private set; }
    public string? InteriorColor { get; private set; }
    public string? Engine { get; private set; }
    public int? Doors { get; private set; }
    public int? Seats { get; private set; }
    public string? DealerName { get; private set; }
    public Guid? DealerId { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? ListingUrl { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyList<string> ImageUrls { get; private set; } = [];
    public string? PrimaryImageHash { get; private set; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset? LastUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? VehicleId { get; private set; }

    private readonly List<PriceHistory> _priceHistory = [];
    public IReadOnlyList<PriceHistory> PriceHistory => _priceHistory.AsReadOnly();

    private Listing() { }

    public static Listing Create(
        string sourceListingId,
        string source,
        string title,
        decimal? price,
        string? make = null,
        string? model = null,
        int? year = null)
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            SourceListingId = sourceListingId,
            Source = source,
            Title = title,
            Price = price,
            Make = make,
            Model = model,
            Year = year,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        if (price.HasValue)
        {
            listing._priceHistory.Add(new PriceHistory
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                Price = price.Value,
                RecordedAt = DateTimeOffset.UtcNow
            });
        }

        return listing;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (Price != newPrice)
        {
            Price = newPrice;
            LastUpdatedAt = DateTimeOffset.UtcNow;

            _priceHistory.Add(new PriceHistory
            {
                Id = Guid.NewGuid(),
                ListingId = Id,
                Price = newPrice,
                RecordedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public void Update(
        string? title = null,
        int? mileage = null,
        string? vin = null,
        string? description = null,
        IReadOnlyList<string>? imageUrls = null)
    {
        if (title != null) Title = title;
        if (mileage != null) Mileage = mileage;
        if (vin != null) VIN = vin;
        if (description != null) Description = description;
        if (imageUrls != null) ImageUrls = imageUrls;

        LastSeenAt = DateTimeOffset.UtcNow;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetVehicleDetails(
        Condition? condition = null,
        BodyStyle? bodyStyle = null,
        Transmission? transmission = null,
        FuelType? fuelType = null,
        Drivetrain? drivetrain = null,
        string? exteriorColor = null,
        string? interiorColor = null,
        string? engine = null,
        int? doors = null,
        int? seats = null)
    {
        Condition = condition;
        BodyStyle = bodyStyle;
        Transmission = transmission;
        FuelType = fuelType;
        Drivetrain = drivetrain;
        ExteriorColor = exteriorColor;
        InteriorColor = interiorColor;
        Engine = engine;
        Doors = doors;
        Seats = seats;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLocation(
        string? city = null,
        string? province = null,
        string? postalCode = null,
        double? latitude = null,
        double? longitude = null)
    {
        City = city;
        Province = province;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void SetDealer(string? dealerName, Guid? dealerId = null)
    {
        DealerName = dealerName;
        DealerId = dealerId;
    }

    public void SetImageHash(string hash)
    {
        PrimaryImageHash = hash;
    }

    public void LinkToVehicle(Guid vehicleId)
    {
        VehicleId = vehicleId;
    }

    public void MarkAsSeen()
    {
        LastSeenAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        LastSeenAt = DateTimeOffset.UtcNow;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }
}
