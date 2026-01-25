using Microsoft.AspNetCore.Mvc;
using Search.Api.DTOs;
using Search.Core.Entities;
using Search.Core.Interfaces;

namespace Search.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ISearchProfileService _profileService;

    public SearchController(ISearchService searchService, ISearchProfileService profileService)
    {
        _searchService = searchService;
        _profileService = profileService;
    }

    /// <summary>
    /// Searches for vehicles based on the given criteria.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SearchResponse>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        var criteria = MapToCriteria(request);
        var result = await _searchService.SearchAsync(criteria, request.Skip, request.Take, cancellationToken);

        return Ok(new SearchResponse(
            result.Items.Select(MapToItemResponse).ToList(),
            result.TotalCount,
            result.Skip,
            result.Take,
            result.SearchDuration.TotalMilliseconds));
    }

    /// <summary>
    /// Gets autocomplete suggestions for a given field.
    /// </summary>
    [HttpGet("autocomplete")]
    public async Task<ActionResult<IEnumerable<AutoCompleteSuggestionResponse>>> GetAutoComplete(
        [FromQuery] string field,
        [FromQuery] string query,
        [FromQuery] int maxSuggestions = 10,
        CancellationToken cancellationToken = default)
    {
        var request = new AutoCompleteRequest
        {
            Field = field,
            Query = query,
            MaxSuggestions = maxSuggestions
        };

        var suggestions = await _searchService.GetAutoCompleteAsync(request, cancellationToken);
        return Ok(suggestions.Select(s => new AutoCompleteSuggestionResponse(s.Value, s.DisplayText, s.Count)));
    }

    /// <summary>
    /// Gets faceted search options based on the current criteria.
    /// </summary>
    [HttpPost("facets")]
    public async Task<ActionResult<SearchFacetsResponse>> GetFacets([FromBody] SearchCriteriaDto? request, CancellationToken cancellationToken)
    {
        var criteria = request != null ? MapToCriteria(request) : SearchCriteria.Empty;
        var facets = await _searchService.GetFacetsAsync(criteria, cancellationToken);

        return Ok(new SearchFacetsResponse(
            facets.Makes.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.Models.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.Years.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.BodyStyles.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.Transmissions.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.Drivetrains.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.FuelTypes.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            facets.Colors.Select(f => new FacetValueResponse(f.Value, f.Count)).ToList(),
            new PriceRangeResponse(facets.PriceRange.Min, facets.PriceRange.Max),
            new MileageRangeResponse(facets.MileageRange.Min, facets.MileageRange.Max)));
    }

    // ========== Search Profile CRUD ==========

    /// <summary>
    /// Gets a search profile by ID.
    /// </summary>
    [HttpGet("profiles/{id:guid}")]
    public async Task<ActionResult<SearchProfileResponse>> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetByIdAsync(id, cancellationToken);
        if (profile == null) return NotFound();
        return Ok(MapToProfileResponse(profile));
    }

    /// <summary>
    /// Gets all search profiles for a user.
    /// </summary>
    [HttpGet("profiles/user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<SearchProfileResponse>>> GetProfilesByUser(Guid userId, CancellationToken cancellationToken)
    {
        var profiles = await _profileService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(profiles.Select(MapToProfileResponse));
    }

    /// <summary>
    /// Gets all active search profiles.
    /// </summary>
    [HttpGet("profiles/active")]
    public async Task<ActionResult<IEnumerable<SearchProfileResponse>>> GetActiveProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _profileService.GetActiveProfilesAsync(cancellationToken);
        return Ok(profiles.Select(MapToProfileResponse));
    }

    /// <summary>
    /// Creates a new search profile.
    /// </summary>
    [HttpPost("profiles")]
    public async Task<ActionResult<SearchProfileResponse>> CreateProfile([FromBody] CreateSearchProfileRequest request, CancellationToken cancellationToken)
    {
        var criteria = MapToCriteria(request.Criteria);
        var profile = await _profileService.CreateAsync(request.Name, criteria, request.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, MapToProfileResponse(profile));
    }

    /// <summary>
    /// Updates an existing search profile.
    /// </summary>
    [HttpPut("profiles/{id:guid}")]
    public async Task<ActionResult<SearchProfileResponse>> UpdateProfile(Guid id, [FromBody] UpdateSearchProfileRequest request, CancellationToken cancellationToken)
    {
        var criteria = request.Criteria != null ? MapToCriteria(request.Criteria) : null;
        var profile = await _profileService.UpdateAsync(id, request.Name, criteria, cancellationToken);
        return Ok(MapToProfileResponse(profile));
    }

    /// <summary>
    /// Deletes a search profile.
    /// </summary>
    [HttpDelete("profiles/{id:guid}")]
    public async Task<IActionResult> DeleteProfile(Guid id, CancellationToken cancellationToken)
    {
        await _profileService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Activates a search profile.
    /// </summary>
    [HttpPost("profiles/{id:guid}/activate")]
    public async Task<IActionResult> ActivateProfile(Guid id, CancellationToken cancellationToken)
    {
        await _profileService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deactivates a search profile.
    /// </summary>
    [HttpPost("profiles/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateProfile(Guid id, CancellationToken cancellationToken)
    {
        await _profileService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    // ========== Mapping Methods ==========

    private static SearchCriteria MapToCriteria(SearchRequest request)
    {
        return new SearchCriteria
        {
            Make = request.Make,
            Model = request.Model,
            YearFrom = request.YearFrom,
            YearTo = request.YearTo,
            PriceFrom = request.PriceFrom,
            PriceTo = request.PriceTo,
            MileageFrom = request.MileageFrom,
            MileageTo = request.MileageTo,
            BodyStyle = request.BodyStyle,
            Transmission = request.Transmission,
            Drivetrain = request.Drivetrain,
            FuelType = request.FuelType,
            ExteriorColor = request.ExteriorColor,
            Province = request.Province,
            City = request.City,
            RadiusKm = request.RadiusKm,
            Keywords = request.Keywords,
            CertifiedPreOwned = request.CertifiedPreOwned,
            DealerOnly = request.DealerOnly,
            PrivateSellerOnly = request.PrivateSellerOnly
        };
    }

    private static SearchCriteria MapToCriteria(SearchCriteriaDto dto)
    {
        return new SearchCriteria
        {
            Make = dto.Make,
            Model = dto.Model,
            YearFrom = dto.YearFrom,
            YearTo = dto.YearTo,
            PriceFrom = dto.PriceFrom,
            PriceTo = dto.PriceTo,
            MileageFrom = dto.MileageFrom,
            MileageTo = dto.MileageTo,
            BodyStyle = dto.BodyStyle,
            Transmission = dto.Transmission,
            Drivetrain = dto.Drivetrain,
            FuelType = dto.FuelType,
            ExteriorColor = dto.ExteriorColor,
            Province = dto.Province,
            City = dto.City,
            RadiusKm = dto.RadiusKm,
            Keywords = dto.Keywords,
            CertifiedPreOwned = dto.CertifiedPreOwned,
            DealerOnly = dto.DealerOnly,
            PrivateSellerOnly = dto.PrivateSellerOnly
        };
    }

    private static SearchCriteriaDto MapToCriteriaDto(SearchCriteria criteria)
    {
        return new SearchCriteriaDto(
            criteria.Make,
            criteria.Model,
            criteria.YearFrom,
            criteria.YearTo,
            criteria.PriceFrom,
            criteria.PriceTo,
            criteria.MileageFrom,
            criteria.MileageTo,
            criteria.BodyStyle,
            criteria.Transmission,
            criteria.Drivetrain,
            criteria.FuelType,
            criteria.ExteriorColor,
            criteria.Province,
            criteria.City,
            criteria.RadiusKm,
            criteria.Keywords,
            criteria.CertifiedPreOwned,
            criteria.DealerOnly,
            criteria.PrivateSellerOnly);
    }

    private static SearchResultItemResponse MapToItemResponse(SearchResultItem item)
    {
        return new SearchResultItemResponse(
            item.VehicleId,
            item.ListingId,
            item.Make,
            item.Model,
            item.Year,
            item.Trim,
            item.Price,
            item.Mileage,
            item.BodyStyle,
            item.Transmission,
            item.Drivetrain,
            item.FuelType,
            item.ExteriorColor,
            item.Location,
            item.DealerName,
            item.ImageUrl,
            item.Score);
    }

    private static SearchProfileResponse MapToProfileResponse(SearchProfile profile)
    {
        return new SearchProfileResponse(
            profile.Id,
            profile.Name,
            MapToCriteriaDto(profile.Criteria),
            profile.IsActive,
            profile.UserId,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
