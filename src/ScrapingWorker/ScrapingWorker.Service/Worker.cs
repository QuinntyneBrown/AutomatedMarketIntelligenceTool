using ScrapingOrchestration.Core.Events;
using ScrapingWorker.Core.Models;
using ScrapingWorker.Core.Services;
using Shared.Messaging;

namespace ScrapingWorker.Service;

/// <summary>
/// Background worker that processes scraping jobs from the queue.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly IScraperFactory _scraperFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<Worker> _logger;
    private readonly string _workerId;

    public Worker(
        IScraperFactory scraperFactory,
        IEventPublisher eventPublisher,
        ILogger<Worker> logger)
    {
        _scraperFactory = scraperFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}".Substring(0, 32);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scraping worker {WorkerId} starting", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // In production, this would subscribe to job events from RabbitMQ
                // For now, we just wait for jobs
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Scraping worker {WorkerId} stopping", _workerId);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scraping worker {WorkerId}", _workerId);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Processes a scraping job event.
    /// </summary>
    public async Task HandleJobCreatedAsync(ScrapingJobCreatedEvent jobEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing job {JobId} for source {Source}",
            jobEvent.JobId, jobEvent.Source);

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            if (!_scraperFactory.HasScraper(jobEvent.Source))
            {
                _logger.LogWarning("No scraper available for source {Source}", jobEvent.Source);
                await PublishFailedEventAsync(jobEvent, "No scraper available for this source", startTime, cancellationToken);
                return;
            }

            var scraper = _scraperFactory.GetScraper(jobEvent.Source);
            var progress = new Progress<ScrapeProgress>(p =>
            {
                _logger.LogDebug(
                    "Job {JobId} progress: page {Page}/{Total}, {Listings} listings",
                    jobEvent.JobId, p.CurrentPage, p.TotalPages, p.ListingsScraped);
            });

            var result = await scraper.ScrapeAsync(jobEvent.Parameters, progress, cancellationToken);

            if (result.Success)
            {
                // Publish listing events for each scraped listing
                foreach (var listing in result.Listings)
                {
                    await _eventPublisher.PublishAsync(new ListingScrapedEvent
                    {
                        JobId = jobEvent.JobId,
                        SessionId = jobEvent.SessionId,
                        Source = jobEvent.Source,
                        SourceListingId = listing.SourceListingId,
                        Title = listing.Title,
                        Price = listing.Price,
                        Make = listing.Make,
                        Model = listing.Model,
                        Year = listing.Year,
                        Mileage = listing.Mileage,
                        VIN = listing.VIN,
                        DealerName = listing.DealerName,
                        Location = $"{listing.City}, {listing.Province}",
                        ListingUrl = listing.ListingUrl,
                        ImageUrls = listing.ImageUrls.ToList(),
                        ScrapedAt = listing.ScrapedAt
                    }, cancellationToken);
                }

                // Publish completed event
                await _eventPublisher.PublishAsync(new ScrapingJobCompletedEvent
                {
                    JobId = jobEvent.JobId,
                    SessionId = jobEvent.SessionId,
                    Source = jobEvent.Source,
                    ListingsFound = result.TotalListingsFound,
                    PagesCrawled = result.PagesCrawled,
                    Duration = result.Duration
                }, cancellationToken);

                _logger.LogInformation(
                    "Job {JobId} completed: {Listings} listings in {Duration}ms",
                    jobEvent.JobId, result.TotalListingsFound, result.Duration.TotalMilliseconds);
            }
            else
            {
                await PublishFailedEventAsync(jobEvent, result.ErrorMessage ?? "Unknown error", startTime, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed with exception", jobEvent.JobId);
            await PublishFailedEventAsync(jobEvent, ex.Message, startTime, cancellationToken);
        }
    }

    private async Task PublishFailedEventAsync(
        ScrapingJobCreatedEvent jobEvent,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        await _eventPublisher.PublishAsync(new ScrapingJobFailedEvent
        {
            JobId = jobEvent.JobId,
            SessionId = jobEvent.SessionId,
            Source = jobEvent.Source,
            ErrorMessage = errorMessage,
            RetryCount = 0,
            WillRetry = false
        }, cancellationToken);
    }
}
