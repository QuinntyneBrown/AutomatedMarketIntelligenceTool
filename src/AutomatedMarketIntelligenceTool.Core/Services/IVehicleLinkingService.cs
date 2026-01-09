using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for linking listings to vehicle entities across sources.
/// </summary>
public interface IVehicleLinkingService
{
    /// <summary>
    /// Links a listing to a vehicle, creating a new vehicle if necessary.
    /// </summary>
    /// <param name="listingId">ID of the listing to link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vehicle ID that the listing was linked to.</returns>
    Task<Guid> LinkListingToVehicleAsync(Guid listingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates vehicle statistics based on linked listings.
    /// </summary>
    /// <param name="vehicleId">ID of the vehicle to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateVehicleStatisticsAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
