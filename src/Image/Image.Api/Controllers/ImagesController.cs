using Image.Api.DTOs;
using Image.Core.Entities;
using Image.Core.Events;
using Image.Core.Services;
using Image.Infrastructure.Data;
using Image.Infrastructure.Hashing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;
using SixLabors.ImageSharp;

namespace Image.Api.Controllers;

/// <summary>
/// API controller for image operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ImagesController : ControllerBase
{
    private readonly ImageDbContext _dbContext;
    private readonly IImageHashingService _hashingService;
    private readonly IImageDownloadService _downloadService;
    private readonly IBlobStorageService _blobStorage;
    private readonly PerceptualHashCalculator _hashCalculator;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        ImageDbContext dbContext,
        IImageHashingService hashingService,
        IImageDownloadService downloadService,
        IBlobStorageService blobStorage,
        PerceptualHashCalculator hashCalculator,
        IEventPublisher eventPublisher,
        ILogger<ImagesController> logger)
    {
        _dbContext = dbContext;
        _hashingService = hashingService;
        _downloadService = downloadService;
        _blobStorage = blobStorage;
        _hashCalculator = hashCalculator;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Uploads an image and calculates its hash.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ImageHashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImageHashResponse>> Upload([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null && string.IsNullOrWhiteSpace(request.SourceUrl))
        {
            return BadRequest("Either File or SourceUrl must be provided");
        }

        byte[]? imageBytes;
        string sourceUrl;

        if (request.File != null)
        {
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream, cancellationToken);
            imageBytes = memoryStream.ToArray();
            sourceUrl = request.File.FileName;
        }
        else
        {
            imageBytes = await _downloadService.DownloadAsync(request.SourceUrl!, cancellationToken);
            sourceUrl = request.SourceUrl!;
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            return BadRequest("Failed to obtain image data");
        }

        return await ProcessAndStoreImage(imageBytes, sourceUrl, request.ListingId, cancellationToken);
    }

    /// <summary>
    /// Calculates the hash of an image from a URL.
    /// </summary>
    [HttpPost("hash")]
    [ProducesResponseType(typeof(ImageHashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImageHashResponse>> CalculateHash([FromBody] HashImageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest("ImageUrl is required");
        }

        var imageBytes = await _downloadService.DownloadAsync(request.ImageUrl, cancellationToken);
        if (imageBytes == null)
        {
            return Ok(new ImageHashResponse
            {
                Id = Guid.NewGuid(),
                SourceUrl = request.ImageUrl,
                Hash = null,
                Success = false,
                ErrorMessage = "Failed to download image",
                ListingId = request.ListingId,
                ProcessedAt = DateTimeOffset.UtcNow
            });
        }

        return await ProcessAndStoreImage(imageBytes, request.ImageUrl, request.ListingId, cancellationToken);
    }

    /// <summary>
    /// Compares two image hashes and returns similarity.
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(HashComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<HashComparisonResponse> CompareHashes([FromBody] CompareHashesRequest request, [FromQuery] double threshold = 0.9)
    {
        if (string.IsNullOrWhiteSpace(request.Hash1) || string.IsNullOrWhiteSpace(request.Hash2))
        {
            return BadRequest("Both hashes are required");
        }

        var similarity = _hashingService.CompareHashes(request.Hash1, request.Hash2);

        return Ok(new HashComparisonResponse
        {
            Hash1 = request.Hash1,
            Hash2 = request.Hash2,
            Similarity = similarity,
            AreSimilar = similarity >= threshold,
            Threshold = threshold
        });
    }

    /// <summary>
    /// Gets an image by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImageResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var image = await _dbContext.Images.FindAsync([id], cancellationToken);
        if (image == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(image));
    }

    /// <summary>
    /// Gets images by listing ID.
    /// </summary>
    [HttpGet("by-listing/{listingId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ImageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ImageResponse>>> GetByListingId(Guid listingId, CancellationToken cancellationToken)
    {
        var images = await _dbContext.Images
            .Where(x => x.ListingId == listingId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(images.Select(MapToResponse));
    }

    /// <summary>
    /// Finds similar images by hash.
    /// </summary>
    [HttpGet("similar/{hash}")]
    [ProducesResponseType(typeof(IEnumerable<ImageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ImageResponse>>> FindSimilar(string hash, [FromQuery] double threshold = 0.9, CancellationToken cancellationToken = default)
    {
        var allImages = await _dbContext.Images
            .Where(x => x.IsProcessed && x.Hash != null)
            .ToListAsync(cancellationToken);

        var similar = allImages
            .Where(img => _hashingService.AreSimilar(hash, img.Hash!, threshold))
            .Select(MapToResponse)
            .ToList();

        return Ok(similar);
    }

    private async Task<ActionResult<ImageHashResponse>> ProcessAndStoreImage(
        byte[] imageBytes,
        string sourceUrl,
        Guid? listingId,
        CancellationToken cancellationToken)
    {
        var imageId = Guid.NewGuid();
        string? hash = null;
        string? errorMessage = null;
        int? width = null;
        int? height = null;
        string? contentType = null;
        string? storagePath = null;

        try
        {
            // Get image info
            var imageInfo = SixLabors.ImageSharp.Image.Identify(imageBytes);
            if (imageInfo != null)
            {
                width = imageInfo.Width;
                height = imageInfo.Height;
                contentType = imageInfo.Metadata.DecodedImageFormat?.DefaultMimeType;
            }

            // Calculate hash
            hash = _hashCalculator.CalculateHash(imageBytes);

            // Store image
            var fileName = $"{imageId}{GetExtensionFromContentType(contentType)}";
            storagePath = await _blobStorage.UploadAsync(imageBytes, fileName, contentType ?? "image/jpeg", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image from {SourceUrl}", sourceUrl);
            errorMessage = ex.Message;
        }

        var imageRecord = new ImageRecord
        {
            Id = imageId,
            SourceUrl = sourceUrl,
            Hash = hash ?? string.Empty,
            ListingId = listingId,
            FileSizeBytes = imageBytes.Length,
            Width = width,
            Height = height,
            ContentType = contentType,
            StoragePath = storagePath,
            CreatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = DateTimeOffset.UtcNow,
            IsProcessed = hash != null,
            ErrorMessage = errorMessage
        };

        _dbContext.Images.Add(imageRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish events
        if (hash != null)
        {
            await _eventPublisher.PublishAsync(new ImageHashCalculatedEvent
            {
                ImageId = imageId,
                SourceUrl = sourceUrl,
                Hash = hash,
                ListingId = listingId
            }, cancellationToken);
        }

        await _eventPublisher.PublishAsync(new ImageProcessedEvent
        {
            ImageId = imageId,
            SourceUrl = sourceUrl,
            Hash = hash,
            Success = hash != null,
            ErrorMessage = errorMessage
        }, cancellationToken);

        return Ok(new ImageHashResponse
        {
            Id = imageId,
            SourceUrl = sourceUrl,
            Hash = hash,
            Success = hash != null,
            ErrorMessage = errorMessage,
            ListingId = listingId,
            ProcessedAt = DateTimeOffset.UtcNow
        });
    }

    private ImageResponse MapToResponse(ImageRecord image)
    {
        return new ImageResponse
        {
            Id = image.Id,
            SourceUrl = image.SourceUrl,
            Hash = image.Hash,
            ListingId = image.ListingId,
            FileSizeBytes = image.FileSizeBytes,
            Width = image.Width,
            Height = image.Height,
            ContentType = image.ContentType,
            StorageUrl = image.StoragePath != null ? _blobStorage.GetPublicUrl(image.StoragePath) : null,
            CreatedAt = image.CreatedAt,
            ProcessedAt = image.ProcessedAt,
            IsProcessed = image.IsProcessed
        };
    }

    private static string GetExtensionFromContentType(string? contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            _ => ".jpg"
        };
    }
}
