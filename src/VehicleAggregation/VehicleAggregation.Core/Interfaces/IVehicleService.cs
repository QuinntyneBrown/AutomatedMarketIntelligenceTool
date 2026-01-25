using VehicleAggregation.Core.Entities;

namespace VehicleAggregation.Core.Interfaces;

public interface IVehicleService
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetByVinAsync(string vin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> SearchAsync(VehicleSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<Vehicle> CreateOrUpdateAsync(VehicleData data, CancellationToken cancellationToken = default);
    Task<Vehicle> LinkListingAsync(Guid vehicleId, Guid listingId, decimal price, string source, string? dealerName, CancellationToken cancellationToken = default);
    Task UnlinkListingAsync(Guid vehicleId, Guid listingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetByMakeModelYearAsync(string make, string model, int year, CancellationToken cancellationToken = default);
}

public sealed record VehicleSearchCriteria
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? PriceFrom { get; init; }
    public decimal? PriceTo { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

public sealed record VehicleData
{
    public string? VIN { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Trim { get; init; }
    public string? BodyStyle { get; init; }
    public string? Transmission { get; init; }
    public string? Drivetrain { get; init; }
    public string? FuelType { get; init; }
    public string? ExteriorColor { get; init; }
    public string? InteriorColor { get; init; }
}
