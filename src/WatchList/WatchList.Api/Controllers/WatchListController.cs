using Microsoft.AspNetCore.Mvc;
using WatchList.Api.DTOs;
using WatchList.Core.Entities;
using WatchList.Core.Interfaces;

namespace WatchList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchListController : ControllerBase
{
    private readonly IWatchListService _watchListService;

    public WatchListController(IWatchListService watchListService)
    {
        _watchListService = watchListService;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<WatchedListingResponse>>> GetWatchedListings(Guid userId, CancellationToken cancellationToken)
    {
        var listings = await _watchListService.GetWatchedListingsAsync(userId, cancellationToken);
        return Ok(listings.Select(MapToResponse));
    }

    [HttpGet("user/{userId:guid}/listing/{listingId:guid}")]
    public async Task<ActionResult<WatchedListingResponse>> GetWatchedListing(Guid userId, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _watchListService.GetWatchedListingAsync(userId, listingId, cancellationToken);
        if (listing == null) return NotFound();
        return Ok(MapToResponse(listing));
    }

    [HttpPost]
    public async Task<ActionResult<WatchedListingResponse>> AddToWatchList([FromBody] AddToWatchListRequest request, CancellationToken cancellationToken)
    {
        var watchedListing = await _watchListService.AddToWatchListAsync(
            request.UserId,
            request.ListingId,
            request.CurrentPrice,
            request.Notes,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetWatchedListing),
            new { userId = watchedListing.UserId, listingId = watchedListing.ListingId },
            MapToResponse(watchedListing));
    }

    [HttpDelete("user/{userId:guid}/listing/{listingId:guid}")]
    public async Task<IActionResult> RemoveFromWatchList(Guid userId, Guid listingId, CancellationToken cancellationToken)
    {
        await _watchListService.RemoveFromWatchListAsync(userId, listingId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("user/{userId:guid}/listing/{listingId:guid}/notes")]
    public async Task<IActionResult> UpdateNotes(Guid userId, Guid listingId, [FromBody] UpdateNotesRequest request, CancellationToken cancellationToken)
    {
        await _watchListService.UpdateNotesAsync(userId, listingId, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpGet("user/{userId:guid}/changes")]
    public async Task<ActionResult<IEnumerable<PriceChangeResponse>>> GetChanges(Guid userId, CancellationToken cancellationToken)
    {
        var changes = await _watchListService.GetChangesAsync(userId, cancellationToken);
        return Ok(changes.Select(MapToPriceChangeResponse));
    }

    [HttpPost("listing/{listingId:guid}/price")]
    public async Task<IActionResult> UpdateListingPrice(Guid listingId, [FromBody] decimal newPrice, CancellationToken cancellationToken)
    {
        await _watchListService.UpdateWatchedListingPriceAsync(listingId, newPrice, cancellationToken);
        return NoContent();
    }

    private static WatchedListingResponse MapToResponse(WatchedListing listing)
    {
        return new WatchedListingResponse(
            listing.Id,
            listing.UserId,
            listing.ListingId,
            listing.Notes,
            listing.PriceAtWatch,
            listing.CurrentPrice,
            listing.PriceChange,
            listing.AddedAt,
            listing.LastCheckedAt);
    }

    private static PriceChangeResponse MapToPriceChangeResponse(WatchedListing listing)
    {
        var percentChange = listing.PriceAtWatch != 0
            ? (listing.PriceChange / listing.PriceAtWatch) * 100
            : 0;

        return new PriceChangeResponse(
            listing.Id,
            listing.ListingId,
            listing.PriceAtWatch,
            listing.CurrentPrice,
            listing.PriceChange,
            Math.Round(percentChange, 2),
            listing.AddedAt);
    }
}
