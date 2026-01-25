import { Component, signal, computed, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';

interface CarListing {
  id: string;
  title: string;
  price: number;
  mileage: number;
  location: string;
  source: string;
  sourceId: string;
  listedDate: string;
  transmission: string;
  drivetrain: string;
  fuelType: string;
  exteriorColor: string;
  url: string;
}

interface Scraper {
  id: string;
  name: string;
  status: 'pending' | 'running' | 'done' | 'error';
}

interface SearchCriteria {
  make: string;
  year: string;
  model: string;
}

@Component({
  selector: 'app-car-search',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatChipsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatMenuModule,
  ],
  templateUrl: './car-search.html',
  styleUrl: './car-search.scss',
})
export class CarSearchComponent {
  // Form state
  protected readonly selectedMake = signal<string>('');
  protected readonly selectedYear = signal<string>('');
  protected readonly selectedModel = signal<string>('');
  protected readonly yearInput = signal<string>('');

  // Search state
  protected readonly isSearching = signal(false);
  protected readonly searchProgress = signal(0);
  protected readonly loadingMessage = signal('');
  protected readonly hasSearched = signal(false);

  // Results
  protected readonly results = signal<CarListing[]>([]);
  protected readonly sortBy = signal<string>('price-asc');

  // Scrapers
  protected readonly scrapers = signal<Scraper[]>([
    { id: 'autotrader', name: 'Autotrader.ca', status: 'pending' },
    { id: 'kijiji', name: 'Kijiji.ca', status: 'pending' },
    { id: 'cargurus', name: 'CarGurus', status: 'pending' },
    { id: 'clutch', name: 'Clutch.ca', status: 'pending' },
    { id: 'auto123', name: 'Auto123', status: 'pending' },
    { id: 'carfax', name: 'CarFax', status: 'pending' },
    { id: 'carmax', name: 'CarMax', status: 'pending' },
    { id: 'carvana', name: 'Carvana', status: 'pending' },
    { id: 'truecar', name: 'TrueCar', status: 'pending' },
    { id: 'vroom', name: 'Vroom', status: 'pending' },
    { id: 'tabangi', name: 'Tabangi Motors', status: 'pending' },
  ]);

  // Car makes
  protected readonly makes = signal<string[]>([
    'Acura', 'Audi', 'BMW', 'Chevrolet', 'Dodge', 'Ford', 'Honda',
    'Hyundai', 'Jeep', 'Kia', 'Lexus', 'Mazda', 'Mercedes-Benz',
    'Nissan', 'RAM', 'Subaru', 'Tesla', 'Toyota', 'Volkswagen', 'Volvo',
  ]);

  // Models by make
  protected readonly modelsByMake: Record<string, string[]> = {
    'Acura': ['ILX', 'Integra', 'MDX', 'RDX', 'TLX', 'NSX'],
    'Audi': ['A3', 'A4', 'A5', 'A6', 'A7', 'A8', 'Q3', 'Q5', 'Q7', 'Q8', 'e-tron', 'RS5', 'RS7'],
    'BMW': ['2 Series', '3 Series', '4 Series', '5 Series', '7 Series', 'X1', 'X3', 'X5', 'X7', 'M3', 'M5', 'i4', 'iX'],
    'Chevrolet': ['Blazer', 'Camaro', 'Colorado', 'Corvette', 'Equinox', 'Malibu', 'Silverado', 'Suburban', 'Tahoe', 'Trailblazer', 'Traverse'],
    'Dodge': ['Challenger', 'Charger', 'Durango', 'Hornet'],
    'Ford': ['Bronco', 'Edge', 'Escape', 'Explorer', 'F-150', 'Maverick', 'Mustang', 'Ranger'],
    'Honda': ['Accord', 'Civic', 'CR-V', 'HR-V', 'Odyssey', 'Passport', 'Pilot', 'Ridgeline'],
    'Hyundai': ['Elantra', 'Ioniq', 'Kona', 'Palisade', 'Santa Fe', 'Sonata', 'Tucson', 'Venue'],
    'Jeep': ['Cherokee', 'Compass', 'Gladiator', 'Grand Cherokee', 'Wagoneer', 'Wrangler'],
    'Kia': ['Carnival', 'EV6', 'Forte', 'K5', 'Niro', 'Seltos', 'Sorento', 'Soul', 'Sportage', 'Telluride'],
    'Lexus': ['ES', 'GX', 'IS', 'LC', 'LS', 'LX', 'NX', 'RX', 'TX', 'UX'],
    'Mazda': ['CX-30', 'CX-5', 'CX-50', 'CX-90', 'Mazda3', 'Mazda6', 'MX-5 Miata'],
    'Mercedes-Benz': ['A-Class', 'C-Class', 'E-Class', 'S-Class', 'GLA', 'GLB', 'GLC', 'GLE', 'GLS', 'EQE', 'EQS'],
    'Nissan': ['Altima', 'Ariya', 'Frontier', 'Kicks', 'Leaf', 'Maxima', 'Murano', 'Pathfinder', 'Rogue', 'Sentra', 'Titan'],
    'RAM': ['1500', '2500', '3500', 'ProMaster'],
    'Subaru': ['Ascent', 'BRZ', 'Crosstrek', 'Forester', 'Impreza', 'Legacy', 'Outback', 'Solterra', 'WRX'],
    'Tesla': ['Model 3', 'Model S', 'Model X', 'Model Y', 'Cybertruck'],
    'Toyota': ['4Runner', 'Camry', 'Corolla', 'GR86', 'Highlander', 'Land Cruiser', 'Prius', 'RAV4', 'Sequoia', 'Sienna', 'Tacoma', 'Tundra', 'Venza'],
    'Volkswagen': ['Atlas', 'Golf', 'ID.4', 'Jetta', 'Passat', 'Taos', 'Tiguan'],
    'Volvo': ['C40', 'S60', 'S90', 'V60', 'V90', 'XC40', 'XC60', 'XC90'],
  };

  // Years (current year down to 1990)
  protected readonly years = signal<number[]>(
    Array.from({ length: new Date().getFullYear() - 1989 }, (_, i) => new Date().getFullYear() - i)
  );

  // Filtered years for autocomplete
  protected readonly filteredYears = computed(() => {
    const input = this.yearInput().trim();
    if (!input) return this.years();
    return this.years().filter(y => y.toString().includes(input));
  });

  // Available models based on selected make
  protected readonly availableModels = computed(() => {
    const make = this.selectedMake();
    return make ? (this.modelsByMake[make] || []) : [];
  });

  // Search summary for results header
  protected readonly searchSummary = computed(() => {
    const year = this.selectedYear();
    const make = this.selectedMake();
    const model = this.selectedModel();
    return `${year} ${make} ${model}`.trim() || '-';
  });

  // Sorted results
  protected readonly sortedResults = computed(() => {
    const items = [...this.results()];
    const sort = this.sortBy();

    switch (sort) {
      case 'price-asc':
        return items.sort((a, b) => a.price - b.price);
      case 'price-desc':
        return items.sort((a, b) => b.price - a.price);
      case 'mileage-asc':
        return items.sort((a, b) => a.mileage - b.mileage);
      case 'mileage-desc':
        return items.sort((a, b) => b.mileage - a.mileage);
      case 'date-desc':
        return items.sort((a, b) => new Date(b.listedDate).getTime() - new Date(a.listedDate).getTime());
      default:
        return items;
    }
  });

  // Form validation
  protected readonly canSearch = computed(() => {
    return this.selectedMake() && this.selectedModel() && !this.isSearching();
  });

  constructor() {
    // Reset model when make changes
    effect(() => {
      const make = this.selectedMake();
      if (make) {
        this.selectedModel.set('');
      }
    }, { allowSignalWrites: true });
  }

  protected onMakeChange(value: string): void {
    this.selectedMake.set(value);
  }

  protected onYearInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.yearInput.set(input.value);
  }

  protected onYearSelected(year: number): void {
    this.selectedYear.set(year.toString());
    this.yearInput.set(year.toString());
  }

  protected onModelChange(value: string): void {
    this.selectedModel.set(value);
  }

  protected onSortChange(value: string): void {
    this.sortBy.set(value);
  }

  protected async onSearch(): Promise<void> {
    if (!this.canSearch()) return;

    const criteria: SearchCriteria = {
      make: this.selectedMake(),
      year: this.selectedYear(),
      model: this.selectedModel(),
    };

    this.isSearching.set(true);
    this.searchProgress.set(0);
    this.loadingMessage.set('Initializing scraping job...');
    this.resetScraperStatuses();

    try {
      // Step 1: Create scraping job via API Gateway
      const jobId = await this.createScrapingJob(criteria);

      // Step 2: Poll for job status and collect results
      const results = await this.pollJobStatus(jobId, criteria);

      // Step 3: Update results
      this.results.set(results);
      this.hasSearched.set(true);
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      this.isSearching.set(false);
    }
  }

  private async createScrapingJob(criteria: SearchCriteria): Promise<string> {
    // In production, this would call:
    // POST /api/v1/scraping/jobs
    // Body: { make, year, model, scrapers: this.scrapers().map(s => s.id) }

    console.log('Creating scraping job:', criteria);
    this.loadingMessage.set('Creating scraping job...');

    // Simulate API delay
    await this.delay(500);

    // Return mock job ID
    return 'job-' + Date.now();
  }

  private async pollJobStatus(jobId: string, criteria: SearchCriteria): Promise<CarListing[]> {
    // In production, this would poll:
    // GET /api/v1/scraping/jobs/{jobId}

    this.loadingMessage.set('Dispatching scrapers...');

    const results: CarListing[] = [];
    const scraperList = this.scrapers();
    let completed = 0;

    for (const scraper of scraperList) {
      // Simulate running state
      await this.delay(200 + Math.random() * 300);
      this.updateScraperStatus(scraper.id, 'running');

      // Simulate scraping
      await this.delay(400 + Math.random() * 800);

      // Generate mock results
      const scraperResults = this.generateMockResults(scraper, criteria, 2 + Math.floor(Math.random() * 5));
      results.push(...scraperResults);

      // Mark as done
      this.updateScraperStatus(scraper.id, 'done');
      completed++;

      // Update progress
      const progress = (completed / scraperList.length) * 100;
      this.searchProgress.set(progress);
      this.loadingMessage.set(`Scraped ${completed}/${scraperList.length} sources (${results.length} listings found)`);
    }

    return results;
  }

  private generateMockResults(scraper: Scraper, criteria: SearchCriteria, count: number): CarListing[] {
    const results: CarListing[] = [];
    const locations = ['Toronto, ON', 'Vancouver, BC', 'Calgary, AB', 'Montreal, QC', 'Ottawa, ON'];
    const transmissions = ['Automatic', 'Manual', 'CVT'];
    const drivetrains = ['FWD', 'RWD', 'AWD', '4WD'];
    const fuelTypes = ['Gasoline', 'Diesel', 'Hybrid', 'Electric'];
    const colors = ['Black', 'White', 'Silver', 'Blue', 'Red', 'Gray'];

    for (let i = 0; i < count; i++) {
      results.push({
        id: `${scraper.id}-${Date.now()}-${i}`,
        title: `${criteria.year || '2023'} ${criteria.make} ${criteria.model}`,
        price: 15000 + Math.floor(Math.random() * 40000),
        mileage: 10000 + Math.floor(Math.random() * 150000),
        location: locations[Math.floor(Math.random() * locations.length)],
        source: scraper.name,
        sourceId: scraper.id,
        listedDate: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
        transmission: transmissions[Math.floor(Math.random() * transmissions.length)],
        drivetrain: drivetrains[Math.floor(Math.random() * drivetrains.length)],
        fuelType: fuelTypes[Math.floor(Math.random() * fuelTypes.length)],
        exteriorColor: colors[Math.floor(Math.random() * colors.length)],
        url: '#',
      });
    }

    return results;
  }

  private updateScraperStatus(scraperId: string, status: Scraper['status']): void {
    this.scrapers.update(scrapers =>
      scrapers.map(s => s.id === scraperId ? { ...s, status } : s)
    );
  }

  private resetScraperStatuses(): void {
    this.scrapers.update(scrapers =>
      scrapers.map(s => ({ ...s, status: 'pending' as const }))
    );
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  protected formatPrice(price: number): string {
    return '$' + price.toLocaleString();
  }

  protected formatMileage(mileage: number): string {
    return mileage.toLocaleString() + ' km';
  }

  protected formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    if (days < 7) return `${days} days ago`;
    if (days < 30) return `${Math.floor(days / 7)} weeks ago`;
    return date.toLocaleDateString();
  }

  protected getScraperStatusClass(status: Scraper['status']): string {
    switch (status) {
      case 'pending': return 'scraper-chip--pending';
      case 'running': return 'scraper-chip--running';
      case 'done': return 'scraper-chip--done';
      case 'error': return 'scraper-chip--error';
      default: return '';
    }
  }

  protected onExport(): void {
    if (this.results().length === 0) return;

    // In production, this would call:
    // POST /api/v1/reports/generate
    // Body: { type: 'search-results', format: 'csv', data: this.results() }

    const csv = this.convertToCSV(this.results());
    this.downloadCSV(csv, 'car-search-results.csv');
  }

  private convertToCSV(results: CarListing[]): string {
    const headers = ['Title', 'Price', 'Mileage', 'Location', 'Source', 'Transmission', 'Drivetrain', 'Fuel Type', 'Listed Date'];
    const rows = results.map(r => [
      r.title,
      r.price.toString(),
      r.mileage.toString(),
      r.location,
      r.source,
      r.transmission,
      r.drivetrain,
      r.fuelType,
      new Date(r.listedDate).toLocaleDateString(),
    ]);

    return [headers, ...rows].map(row => row.map(cell => `"${cell}"`).join(',')).join('\n');
  }

  private downloadCSV(csv: string, filename: string): void {
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
  }

  protected openListing(listing: CarListing): void {
    // In production, this would open the actual listing URL
    console.log('Opening listing:', listing);
    if (listing.url && listing.url !== '#') {
      window.open(listing.url, '_blank');
    }
  }
}
