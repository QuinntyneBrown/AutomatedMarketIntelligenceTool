using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Events;

namespace AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

public class Listing
{
    private readonly List<object> _domainEvents = new();

    public ListingId ListingId { get; private set; }
    public Guid TenantId { get; private set; }
    public string ExternalId { get; private set; }
    public string SourceSite { get; private set; }
    public string ListingUrl { get; private set; }
    public string Make { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }
    public string? Trim { get; private set; }
    public decimal Price { get; private set; }
    public int? Mileage { get; private set; }
    public string? Vin { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public string Currency { get; private set; } = "CAD";
    public Condition Condition { get; private set; }
    public Transmission? Transmission { get; private set; }
    public FuelType? FuelType { get; private set; }
    public BodyStyle? BodyStyle { get; private set; }
    public Drivetrain? Drivetrain { get; private set; }
    public string? ExteriorColor { get; private set; }
    public string? InteriorColor { get; private set; }
    public SellerType? SellerType { get; private set; }
    public string? SellerName { get; private set; }
    public string? SellerPhone { get; private set; }
    public string? Description { get; private set; }
    public List<string> ImageUrls { get; private set; } = new();
    public DateTime? ListingDate { get; private set; }
    public int? DaysOnMarket { get; private set; }
    public DateTime FirstSeenDate { get; private set; }
    public DateTime LastSeenDate { get; private set; }
    public bool IsNewListing { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Listing()
    {
        ListingId = ListingId.Create();
        ExternalId = string.Empty;
        SourceSite = string.Empty;
        ListingUrl = string.Empty;
        Make = string.Empty;
        Model = string.Empty;
    }

    public static Listing Create(
        Guid tenantId,
        string externalId,
        string sourceSite,
        string listingUrl,
        string make,
        string model,
        int year,
        decimal price,
        Condition condition,
        string? trim = null,
        int? mileage = null,
        string? vin = null,
        string? city = null,
        string? province = null,
        string? postalCode = null,
        string? currency = null,
        Transmission? transmission = null,
        FuelType? fuelType = null,
        BodyStyle? bodyStyle = null,
        Drivetrain? drivetrain = null,
        string? exteriorColor = null,
        string? interiorColor = null,
        SellerType? sellerType = null,
        string? sellerName = null,
        string? sellerPhone = null,
        string? description = null,
        List<string>? imageUrls = null,
        DateTime? listingDate = null,
        int? daysOnMarket = null)
    {
        var listing = new Listing
        {
            ListingId = ListingId.Create(),
            TenantId = tenantId,
            ExternalId = externalId,
            SourceSite = sourceSite,
            ListingUrl = listingUrl,
            Make = make,
            Model = model,
            Year = year,
            Trim = trim,
            Price = price,
            Mileage = mileage,
            Vin = vin,
            City = city,
            Province = province,
            PostalCode = postalCode,
            Currency = currency ?? "CAD",
            Condition = condition,
            Transmission = transmission,
            FuelType = fuelType,
            BodyStyle = bodyStyle,
            Drivetrain = drivetrain,
            ExteriorColor = exteriorColor,
            InteriorColor = interiorColor,
            SellerType = sellerType,
            SellerName = sellerName,
            SellerPhone = sellerPhone,
            Description = description,
            ImageUrls = imageUrls ?? new List<string>(),
            ListingDate = listingDate,
            DaysOnMarket = daysOnMarket,
            FirstSeenDate = DateTime.UtcNow,
            LastSeenDate = DateTime.UtcNow,
            IsNewListing = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        listing._domainEvents.Add(new ListingCreated(
            listing.ListingId,
            listing.Make,
            listing.Model,
            listing.Year,
            listing.Price,
            listing.SourceSite,
            listing.CreatedAt));

        return listing;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (Price != newPrice)
        {
            var oldPrice = Price;
            Price = newPrice;
            UpdatedAt = DateTime.UtcNow;
            LastSeenDate = DateTime.UtcNow;

            _domainEvents.Add(new ListingPriceChanged(
                ListingId,
                oldPrice,
                newPrice,
                UpdatedAt.Value));
        }
    }

    public void MarkAsSeen()
    {
        LastSeenDate = DateTime.UtcNow;
        if (IsNewListing)
        {
            IsNewListing = false;
        }
        if (!IsActive)
        {
            Reactivate();
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            DeactivatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Reactivate()
    {
        if (!IsActive)
        {
            IsActive = true;
            DeactivatedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
