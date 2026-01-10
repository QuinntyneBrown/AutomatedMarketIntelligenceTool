using AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public interface IScraperFactory
{
    ISiteScraper CreateScraper(string siteName);
    IEnumerable<ISiteScraper> CreateAllScrapers();
    IEnumerable<string> GetSupportedSites();
}

public class ScraperFactory : IScraperFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ScraperFactory> _logger;
    private readonly IRateLimiter? _rateLimiter;

    public ScraperFactory(ILoggerFactory loggerFactory, IRateLimiter? rateLimiter = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<ScraperFactory>();
        _rateLimiter = rateLimiter;
    }

    public ISiteScraper CreateScraper(string siteName)
    {
        _logger.LogDebug("Creating scraper for site: {SiteName}", siteName);
        
        ISiteScraper scraper = siteName.ToLowerInvariant() switch
        {
            // Major aggregator sites
            "autotrader" or "autotrader.ca" => new AutotraderScraper(_loggerFactory.CreateLogger<AutotraderScraper>()),
            "kijiji" or "kijiji.ca" => new KijijiScraper(_loggerFactory.CreateLogger<KijijiScraper>()),
            "cargurus" or "cargurus.ca" => new CarGurusScraper(_loggerFactory.CreateLogger<CarGurusScraper>()),
            "clutch" or "clutch.ca" => new ClutchScraper(_loggerFactory.CreateLogger<ClutchScraper>()),
            "auto123" or "auto123.com" => new Auto123Scraper(_loggerFactory.CreateLogger<Auto123Scraper>()),
            "carmax" or "carmax.com" => new CarMaxScraper(_loggerFactory.CreateLogger<CarMaxScraper>()),
            "carvana" or "carvana.com" => new CarvanaScraper(_loggerFactory.CreateLogger<CarvanaScraper>()),
            "vroom" or "vroom.com" => new VroomScraper(_loggerFactory.CreateLogger<VroomScraper>()),
            "truecar" or "truecar.com" => new TrueCarScraper(_loggerFactory.CreateLogger<TrueCarScraper>()),
            "carfax" or "carfax.ca" => new CarFaxScraper(_loggerFactory.CreateLogger<CarFaxScraper>()),

            // Toronto dealerships
            "autorama" or "autorama.ca" => new AutoramaScraper(_loggerFactory.CreateLogger<AutoramaScraper>()),
            "nexcar" or "nexcar.ca" => new NexcarScraper(_loggerFactory.CreateLogger<NexcarScraper>()),
            "carconnectiontoronto" or "carconnectiontoronto.ca" => new CarConnectionTorontoScraper(_loggerFactory.CreateLogger<CarConnectionTorontoScraper>()),

            // Mississauga dealerships
            "tabangimotors" or "tabangimotors.com" => new TabangiMotorsScraper(_loggerFactory.CreateLogger<TabangiMotorsScraper>()),
            "autodistrict" or "autodistrict.ca" => new AutoDistrictScraper(_loggerFactory.CreateLogger<AutoDistrictScraper>()),
            "dannyandsons" or "dannyandsons.com" => new DannyAndSonsScraper(_loggerFactory.CreateLogger<DannyAndSonsScraper>()),
            "mississaugaautogroup" or "mississaugaautogroup.com" => new MississaugaAutoGroupScraper(_loggerFactory.CreateLogger<MississaugaAutoGroupScraper>()),

            // Brampton dealerships
            "autoparkbrampton" or "autoparkbrampton.ca" => new AutoParkBramptonScraper(_loggerFactory.CreateLogger<AutoParkBramptonScraper>()),
            "firstmotors" or "firstmotors.ca" => new FirstMotorsScraper(_loggerFactory.CreateLogger<FirstMotorsScraper>()),
            "driftmotors" or "driftmotors.ca" => new DriftMotorsScraper(_loggerFactory.CreateLogger<DriftMotorsScraper>()),

            // Hamilton dealerships
            "globalautomart" or "globalautomart.ca" => new GlobalAutomartScraper(_loggerFactory.CreateLogger<GlobalAutomartScraper>()),
            "waynesautoworld" or "waynesautoworld.ca" => new WaynesAutoWorldScraper(_loggerFactory.CreateLogger<WaynesAutoWorldScraper>()),
            "atlasautomotive" or "atlasautomotive.ca" => new AtlasAutomotiveScraper(_loggerFactory.CreateLogger<AtlasAutomotiveScraper>()),

            // Kitchener-Waterloo dealerships
            "mostwantedcars" or "mostwantedcars.ca" => new MostWantedCarsScraper(_loggerFactory.CreateLogger<MostWantedCarsScraper>()),
            "qualitycarsales" or "qualitycarsales.com" => new QualityCarSalesScraper(_loggerFactory.CreateLogger<QualityCarSalesScraper>()),
            "fitzgeraldmotors" or "fitzgeraldmotors.com" => new FitzgeraldMotorsScraper(_loggerFactory.CreateLogger<FitzgeraldMotorsScraper>()),

            // London dealerships
            "empireatogroup" or "empireautogroup.ca" => new EmpireAutoGroupScraper(_loggerFactory.CreateLogger<EmpireAutoGroupScraper>()),
            "finemotors" or "finemotors.ca" => new FineMotorsLondonScraper(_loggerFactory.CreateLogger<FineMotorsLondonScraper>()),
            "cedarauto" or "cedarauto.ca" => new CedarAutoScraper(_loggerFactory.CreateLogger<CedarAutoScraper>()),

            // Barrie dealerships
            "carcentral" or "carcentral.ca" => new CarCentralScraper(_loggerFactory.CreateLogger<CarCentralScraper>()),
            "gdcoatessuperstore" or "gdcoatessuperstore.ca" => new GDCoatesSuperstoreScraper(_loggerFactory.CreateLogger<GDCoatesSuperstoreScraper>()),
            "autoparkbarrie" or "autoparkbarrie.ca" => new AutoParkBarrieScraper(_loggerFactory.CreateLogger<AutoParkBarrieScraper>()),

            // Oshawa/Durham dealerships
            "autoplanetdurham" or "autoplanetdurham.ca" => new AutoPlanetDurhamScraper(_loggerFactory.CreateLogger<AutoPlanetDurhamScraper>()),
            "bossauto" or "bossauto.ca" => new BossAutoSalesScraper(_loggerFactory.CreateLogger<BossAutoSalesScraper>()),
            "truenorthautomobiles" or "truenorthautomobiles.ca" => new TrueNorthAutomobilesScraper(_loggerFactory.CreateLogger<TrueNorthAutomobilesScraper>()),

            // St. Catharines/Niagara dealerships
            "cmhniagara" or "cmhniagara.com" => new CMHAutoSuperstoreScraper(_loggerFactory.CreateLogger<CMHAutoSuperstoreScraper>()),
            "skywayfinecars" or "skywayfinecars.ca" => new SkywayFineCarsScraper(_loggerFactory.CreateLogger<SkywayFineCarsScraper>()),
            "twoguys" or "twoguys.ca" => new TwoGuysQualityCarsScraper(_loggerFactory.CreateLogger<TwoGuysQualityCarsScraper>()),

            // Guelph dealerships
            "shopwilsons" or "shopwilsons.com" => new MarkWilsonsScraper(_loggerFactory.CreateLogger<MarkWilsonsScraper>()),
            "milburnsautosales" or "milburnsautosales.com" => new MilburnsAutoSalesScraper(_loggerFactory.CreateLogger<MilburnsAutoSalesScraper>()),

            // Cambridge dealerships
            "lebadamotors" or "lebadamotors.com" => new LebadaMotorsScraper(_loggerFactory.CreateLogger<LebadaMotorsScraper>()),
            "oaocars" or "oaocars.com" => new OntarioAutoOutletScraper(_loggerFactory.CreateLogger<OntarioAutoOutletScraper>()),

            // Peterborough dealerships
            "autoconnectsales" or "autoconnectsales.ca" => new AutoConnectSalesScraper(_loggerFactory.CreateLogger<AutoConnectSalesScraper>()),
            "autosource" or "autosource.ca" => new AutosourceScraper(_loggerFactory.CreateLogger<AutosourceScraper>()),

            // Burlington/Oakville dealerships
            "carnationcanadadirect" or "carnationcanadadirect.ca" => new CarNationCanadaScraper(_loggerFactory.CreateLogger<CarNationCanadaScraper>()),
            "knightsautosales" or "knightsautosales.com" => new KnightsAutoSalesScraper(_loggerFactory.CreateLogger<KnightsAutoSalesScraper>()),

            // Richmond Hill/Markham/Vaughan dealerships
            "pfaffauto" or "pfaffauto.com" => new PfaffAutomotiveScraper(_loggerFactory.CreateLogger<PfaffAutomotiveScraper>()),

            _ => throw new ArgumentException($"Unsupported site: {siteName}", nameof(siteName))
        };

        // Apply rate limiting decorator if rate limiter is available
        if (_rateLimiter != null)
        {
            _logger.LogDebug("Applying rate limiting decorator to {SiteName}", siteName);
            scraper = new RateLimitingScraperDecorator(
                scraper,
                _rateLimiter,
                _loggerFactory.CreateLogger<RateLimitingScraperDecorator>());
        }

        _logger.LogInformation("Successfully created scraper for site: {SiteName}", siteName);
        return scraper;
    }

    public IEnumerable<ISiteScraper> CreateAllScrapers()
    {
        _logger.LogInformation("Creating all supported scrapers");

        // Major aggregator sites
        yield return CreateScraper("autotrader");
        yield return CreateScraper("kijiji");
        yield return CreateScraper("cargurus");
        yield return CreateScraper("clutch");
        yield return CreateScraper("auto123");
        yield return CreateScraper("carmax");
        yield return CreateScraper("carvana");
        yield return CreateScraper("vroom");
        yield return CreateScraper("truecar");
        yield return CreateScraper("carfax");

        // Toronto dealerships
        yield return CreateScraper("autorama");
        yield return CreateScraper("nexcar");
        yield return CreateScraper("carconnectiontoronto");

        // Mississauga dealerships
        yield return CreateScraper("tabangimotors");
        yield return CreateScraper("autodistrict");
        yield return CreateScraper("dannyandsons");
        yield return CreateScraper("mississaugaautogroup");

        // Brampton dealerships
        yield return CreateScraper("autoparkbrampton");
        yield return CreateScraper("firstmotors");
        yield return CreateScraper("driftmotors");

        // Hamilton dealerships
        yield return CreateScraper("globalautomart");
        yield return CreateScraper("waynesautoworld");
        yield return CreateScraper("atlasautomotive");

        // Kitchener-Waterloo dealerships
        yield return CreateScraper("mostwantedcars");
        yield return CreateScraper("qualitycarsales");
        yield return CreateScraper("fitzgeraldmotors");

        // London dealerships
        yield return CreateScraper("empireatogroup");
        yield return CreateScraper("finemotors");
        yield return CreateScraper("cedarauto");

        // Barrie dealerships
        yield return CreateScraper("carcentral");
        yield return CreateScraper("gdcoatessuperstore");
        yield return CreateScraper("autoparkbarrie");

        // Oshawa/Durham dealerships
        yield return CreateScraper("autoplanetdurham");
        yield return CreateScraper("bossauto");
        yield return CreateScraper("truenorthautomobiles");

        // St. Catharines/Niagara dealerships
        yield return CreateScraper("cmhniagara");
        yield return CreateScraper("skywayfinecars");
        yield return CreateScraper("twoguys");

        // Guelph dealerships
        yield return CreateScraper("shopwilsons");
        yield return CreateScraper("milburnsautosales");

        // Cambridge dealerships
        yield return CreateScraper("lebadamotors");
        yield return CreateScraper("oaocars");

        // Peterborough dealerships
        yield return CreateScraper("autoconnectsales");
        yield return CreateScraper("autosource");

        // Burlington/Oakville dealerships
        yield return CreateScraper("carnationcanadadirect");
        yield return CreateScraper("knightsautosales");

        // Richmond Hill/Markham/Vaughan dealerships
        yield return CreateScraper("pfaffauto");
    }

    public IEnumerable<string> GetSupportedSites()
    {
        return new[]
        {
            // Major aggregator sites
            "Autotrader.ca",
            "Kijiji.ca",
            "CarGurus.ca",
            "Clutch.ca",
            "Auto123.com",
            "CarMax.com",
            "Carvana.com",
            "Vroom.com",
            "TrueCar.com",
            "CarFax.ca",
            // Toronto dealerships
            "Autorama.ca",
            "Nexcar.ca",
            "CarConnectionToronto.ca",
            // Mississauga dealerships
            "TabangiMotors.com",
            "AutoDistrict.ca",
            "DannyAndSons.com",
            "MississaugaAutoGroup.com",
            // Brampton dealerships
            "AutoParkBrampton.ca",
            "FirstMotors.ca",
            "DriftMotors.ca",
            // Hamilton dealerships
            "GlobalAutomart.ca",
            "WaynesAutoWorld.ca",
            "AtlasAutomotive.ca",
            // Kitchener-Waterloo dealerships
            "MostWantedCars.ca",
            "QualityCarSales.com",
            "FitzgeraldMotors.com",
            // London dealerships
            "EmpireAutoGroup.ca",
            "FineMotors.ca",
            "CedarAuto.ca",
            // Barrie dealerships
            "CarCentral.ca",
            "GDCoatesSuperstore.ca",
            "AutoParkBarrie.ca",
            // Oshawa/Durham dealerships
            "AutoPlanetDurham.ca",
            "BossAuto.ca",
            "TrueNorthAutomobiles.ca",
            // St. Catharines/Niagara dealerships
            "CMHNiagara.com",
            "SkywayFineCars.ca",
            "TwoGuys.ca",
            // Guelph dealerships
            "ShopWilsons.com",
            "MilburnsAutoSales.com",
            // Cambridge dealerships
            "LebadaMotors.com",
            "OAOCars.com",
            // Peterborough dealerships
            "AutoConnectSales.ca",
            "Autosource.ca",
            // Burlington/Oakville dealerships
            "CarNationCanadaDirect.ca",
            "KnightsAutoSales.com",
            // Richmond Hill/Markham/Vaughan dealerships
            "PfaffAuto.com"
        };
    }
}
