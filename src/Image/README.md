# Image Service

The Image Service handles image processing, perceptual hashing, and similarity detection for vehicle listing images.

## Purpose

This microservice is responsible for:
- Downloading and processing vehicle images
- Calculating perceptual hashes for duplicate detection
- Storing images in blob storage
- Comparing image similarity
- Finding visually similar images across listings

## Architecture

```
Image/
├── Image.Api/               # REST API layer
│   └── Controllers/
│       └── ImagesController.cs
├── Image.Core/              # Domain layer
│   ├── Models/
│   │   └── ImageRecord.cs
│   ├── Services/
│   │   ├── IImageHashingService.cs
│   │   ├── IImageDownloadService.cs
│   │   └── IBlobStorageService.cs
│   └── Events/
└── Image.Infrastructure/    # Data access layer
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/images/upload` | Upload and process image |
| POST | `/api/images/hash` | Calculate hash from URL |
| POST | `/api/images/compare` | Compare two image hashes |
| GET | `/api/images/{id}` | Get image by ID |
| GET | `/api/images/by-listing/{listingId}` | Get images for listing |
| GET | `/api/images/similar/{hash}` | Find similar images |

## Domain Model

### ImageRecord
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Unique identifier |
| `SourceUrl` | string | Original image URL |
| `Hash` | string | Perceptual hash |
| `ListingId` | Guid | Associated listing |
| `FileSizeBytes` | long | File size |
| `Width` | int | Image width |
| `Height` | int | Image height |
| `ContentType` | string | MIME type |
| `StoragePath` | string | Blob storage path |
| `CreatedAt` | DateTime | Record creation |
| `ProcessedAt` | DateTime | Processing completion |
| `IsProcessed` | bool | Processing status |
| `ErrorMessage` | string | Error details |

## Core Services

### IImageHashingService
Perceptual hashing algorithm for:
- Generating image fingerprints
- Comparing hash similarity
- Detecting near-duplicate images

### IImageDownloadService
Image download capabilities:
- Download from URLs
- Handle redirects
- Validate image formats
- Size limits enforcement

### IBlobStorageService
Cloud storage integration:
- Upload images to blob storage
- Retrieve stored images
- Generate access URLs
- Manage storage lifecycle

## Supported Formats

| Format | Extension | Support |
|--------|-----------|---------|
| JPEG | .jpg, .jpeg | Full |
| PNG | .png | Full |
| GIF | .gif | Full |
| WebP | .webp | Full |
| BMP | .bmp | Full |

## Integration Events

| Event | Description |
|-------|-------------|
| `ImageHashCalculatedEvent` | Published when hash is computed |
| `ImageProcessedEvent` | Published when processing completes |

## Integration Points

### Inbound
- **Listing Service**: Receives image URLs from new listings
- **Scraping Worker**: Receives scraped image URLs

### Outbound
- **Deduplication Service**: Provides image hashes for duplicate detection
- **Blob Storage**: Stores processed images

## Similarity Threshold

The service uses Hamming distance for hash comparison:
- **0-5**: Identical or near-identical images
- **6-10**: Very similar images
- **11-20**: Somewhat similar images
- **>20**: Different images

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `image-processing.puml` - Image upload and processing flow
- `similarity-detection.puml` - Image comparison flow
