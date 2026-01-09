using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for linking listings to vehicle entities and managing vehicle data.
/// </summary>
public class VehicleLinkingService : IVehicleLinkingService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<VehicleLinkingService> _logger;

    public VehicleLinkingService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<VehicleLinkingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Guid> LinkListingToVehicleAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        var listing = await _context.Listings
            .FirstOrDefaultAsync(l => l.ListingId.Value == listingId, cancellationToken);

        if (listing == null)
        {
            throw new InvalidOperationException($"Listing with ID {listingId} not found.");
        }

        // Check if already linked to a vehicle
        if (listing.LinkedVehicleId.HasValue)
        {
            _logger.LogDebug("Listing {ListingId} already linked to vehicle {VehicleId}", 
                listingId, listing.LinkedVehicleId.Value);
            return listing.LinkedVehicleId.Value;
        }

        // Try to find existing vehicle with same VIN
        Vehicle? vehicle = null;
        if (!string.IsNullOrWhiteSpace(listing.Vin))
        {
            vehicle = await _context.Vehicles
                .Where(v => v.TenantId == listing.TenantId)
                .FirstOrDefaultAsync(
                    v => v.PrimaryVin != null && v.PrimaryVin == listing.Vin,
                    cancellationToken);
        }

        // If no vehicle found by VIN, try to find by make/model/year
        if (vehicle == null)
        {
            vehicle = await _context.Vehicles
                .Where(v => v.TenantId == listing.TenantId)
                .Where(v => v.Make == listing.Make)
                .Where(v => v.Model == listing.Model)
                .Where(v => v.Year == listing.Year)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Create new vehicle if none found
        if (vehicle == null)
        {
            vehicle = Vehicle.Create(
                listing.TenantId,
                listing.Make,
                listing.Model,
                listing.Year,
                listing.Trim,
                listing.Vin);

            _context.Vehicles.Add(vehicle);
            _logger.LogInformation(
                "Created new vehicle {VehicleId} for {Make} {Model} {Year}",
                vehicle.VehicleId.Value,
                vehicle.Make,
                vehicle.Model,
                vehicle.Year);
        }

        // Link listing to vehicle
        listing.SetFuzzyMatchInfo(
            vehicle.VehicleId.Value,
            null,
            "VehicleLink");

        await _context.SaveChangesAsync(cancellationToken);

        // Update vehicle statistics
        await UpdateVehicleStatisticsAsync(vehicle.VehicleId.Value, cancellationToken);

        _logger.LogInformation(
            "Linked listing {ListingId} to vehicle {VehicleId}",
            listingId,
            vehicle.VehicleId.Value);

        return vehicle.VehicleId.Value;
    }

    /// <inheritdoc/>
    public async Task UpdateVehicleStatisticsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.VehicleId.Value == vehicleId, cancellationToken);

        if (vehicle == null)
        {
            throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");
        }

        // Get all active listings linked to this vehicle
        var linkedListings = await _context.Listings
            .Where(l => l.LinkedVehicleId == vehicleId)
            .Where(l => l.IsActive)
            .ToListAsync(cancellationToken);

        if (linkedListings.Any())
        {
            var bestPrice = linkedListings.Min(l => l.Price);
            var averagePrice = linkedListings.Average(l => l.Price);
            var listingCount = linkedListings.Count;

            vehicle.UpdatePrices(bestPrice, averagePrice);
            vehicle.UpdateListingCount(listingCount);
            vehicle.UpdateLastSeenDate();

            // Update primary VIN if we have one
            var vinListing = linkedListings.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.Vin));
            if (vinListing != null && string.IsNullOrWhiteSpace(vehicle.PrimaryVin))
            {
                vehicle.SetPrimaryVin(vinListing.Vin!);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Updated vehicle {VehicleId} statistics: {Count} listings, best price ${BestPrice}",
                vehicleId,
                listingCount,
                bestPrice);
        }
    }
}
