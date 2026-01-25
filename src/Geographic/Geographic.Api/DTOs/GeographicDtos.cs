namespace Geographic.Api.DTOs;

// Request DTOs
public sealed record CreateMarketRequest(
    string Name,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusKm,
    string? PostalCodes);

public sealed record UpdateMarketRequest(
    string Name,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusKm,
    string? PostalCodes);

public sealed record GeocodeRequest(string Address);

public sealed record DistanceRequest(
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude);

public sealed record WithinRadiusRequest(
    double CenterLatitude,
    double CenterLongitude,
    double PointLatitude,
    double PointLongitude,
    double RadiusKm);

// Response DTOs
public sealed record MarketResponse(
    Guid Id,
    string Name,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusKm,
    string? PostalCodes,
    DateTimeOffset CreatedAt);

public sealed record GeoLocationResponse(
    double Latitude,
    double Longitude,
    string? City,
    string? Province,
    string? PostalCode);

public sealed record DistanceResponse(
    GeoLocationResponse FromLocation,
    GeoLocationResponse ToLocation,
    double DistanceKm);

public sealed record WithinRadiusResponse(bool IsWithinRadius, double DistanceKm);
