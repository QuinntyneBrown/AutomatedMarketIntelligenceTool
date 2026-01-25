using Microsoft.EntityFrameworkCore;
using Shared.Messaging;
using VehicleAggregation.Core.Entities;
using VehicleAggregation.Core.Events;
using VehicleAggregation.Core.Interfaces;
using VehicleAggregation.Infrastructure.Data;

namespace VehicleAggregation.Infrastructure.Services;

public sealed class VehicleService : IVehicleService
{
    private readonly VehicleDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public VehicleService(VehicleDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .Include(v => v.Listings)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Vehicle?> GetByVinAsync(string vin, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .Include(v => v.Listings)
            .FirstOrDefaultAsync(v => v.VIN == vin, cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> SearchAsync(VehicleSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.Vehicles.Include(v => v.Listings).AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Make))
            query = query.Where(v => v.Make.ToLower().Contains(criteria.Make.ToLower()));

        if (!string.IsNullOrWhiteSpace(criteria.Model))
            query = query.Where(v => v.Model.ToLower().Contains(criteria.Model.ToLower()));

        if (criteria.YearFrom.HasValue)
            query = query.Where(v => v.Year >= criteria.YearFrom.Value);

        if (criteria.YearTo.HasValue)
            query = query.Where(v => v.Year <= criteria.YearTo.Value);

        if (criteria.PriceFrom.HasValue)
            query = query.Where(v => v.BestPrice >= criteria.PriceFrom.Value);

        if (criteria.PriceTo.HasValue)
            query = query.Where(v => v.BestPrice <= criteria.PriceTo.Value);

        return await query
            .OrderBy(v => v.BestPrice)
            .Skip(criteria.Skip)
            .Take(criteria.Take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vehicle> CreateOrUpdateAsync(VehicleData data, CancellationToken cancellationToken = default)
    {
        Vehicle? vehicle = null;

        if (!string.IsNullOrWhiteSpace(data.VIN))
        {
            vehicle = await GetByVinAsync(data.VIN, cancellationToken);
        }

        if (vehicle == null)
        {
            var existing = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Make == data.Make && v.Model == data.Model && v.Year == data.Year && v.Trim == data.Trim, cancellationToken);
            vehicle = existing;
        }

        if (vehicle == null)
        {
            vehicle = Vehicle.Create(data.Make, data.Model, data.Year, data.VIN, data.Trim);
            _context.Vehicles.Add(vehicle);

            await _context.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new VehicleCreatedEvent
            {
                VehicleId = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                VIN = vehicle.VIN
            }, cancellationToken);
        }
        else
        {
            vehicle.UpdateDetails(data.Trim, data.BodyStyle, data.Transmission, data.Drivetrain, data.FuelType, data.ExteriorColor, data.InteriorColor);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return vehicle;
    }

    public async Task<Vehicle> LinkListingAsync(Guid vehicleId, Guid listingId, decimal price, string source, string? dealerName, CancellationToken cancellationToken = default)
    {
        var vehicle = await GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle {vehicleId} not found");

        var previousBestPrice = vehicle.BestPrice;
        vehicle.AddListing(listingId, price, source, dealerName);

        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new ListingLinkedToVehicleEvent
        {
            VehicleId = vehicleId,
            ListingId = listingId,
            Price = price
        }, cancellationToken);

        if (vehicle.BestPrice.HasValue && (previousBestPrice != vehicle.BestPrice))
        {
            await _eventPublisher.PublishAsync(new BestPriceChangedEvent
            {
                VehicleId = vehicleId,
                PreviousBestPrice = previousBestPrice,
                NewBestPrice = vehicle.BestPrice.Value,
                BestPriceListingId = vehicle.BestPriceListingId!.Value
            }, cancellationToken);
        }

        return vehicle;
    }

    public async Task UnlinkListingAsync(Guid vehicleId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var vehicle = await GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle {vehicleId} not found");

        vehicle.RemoveListing(listingId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> GetByMakeModelYearAsync(string make, string model, int year, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .Include(v => v.Listings)
            .Where(v => v.Make.ToLower() == make.ToLower() && v.Model.ToLower() == model.ToLower() && v.Year == year)
            .ToListAsync(cancellationToken);
    }
}
