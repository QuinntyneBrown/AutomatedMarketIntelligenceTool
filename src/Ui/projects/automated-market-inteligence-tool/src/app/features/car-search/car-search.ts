import { Component, signal, computed, effect, inject, OnDestroy } from '@angular/core';
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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject, takeUntil, interval, switchMap, catchError, of, filter, tap } from 'rxjs';

import {
  ScrapingService,
  ScrapingSource,
  SearchSessionStatus,
  ScrapingJobStatus,
  ScrapingSessionResponse,
  ScrapingJobResponse,
  CarListing,
  CreateScrapingJobRequest,
} from './scraping.service';

interface ScraperStatus {
  id: ScrapingSource;
  name: string;
  status: 'pending' | 'running' | 'done' | 'error';
  listingsFound: number;
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
    MatSnackBarModule,
  ],
  templateUrl: './car-search.html',
  styleUrl: './car-search.scss',
})
export class CarSearchComponent implements OnDestroy {
  private readonly scrapingService = inject(ScrapingService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

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
  protected readonly currentSessionId = signal<string | null>(null);

  // Results
  protected readonly results = signal<CarListing[]>([]);
  protected readonly sortBy = signal<string>('price-asc');
  protected readonly totalListingsFound = signal(0);

  // Scraper statuses
  protected readonly scraperStatuses = signal<ScraperStatus[]>([
    { id: ScrapingSource.Autotrader, name: 'Autotrader.ca', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.Kijiji, name: 'Kijiji.ca', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.CarGurus, name: 'CarGurus', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.Clutch, name: 'Clutch.ca', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.Auto123, name: 'Auto123', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.CarFax, name: 'CarFax', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.CarMax, name: 'CarMax', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.Carvana, name: 'Carvana', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.TrueCar, name: 'TrueCar', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.Vroom, name: 'Vroom', status: 'pending', listingsFound: 0 },
    { id: ScrapingSource.TabangiMotors, name: 'Tabangi Motors', status: 'pending', listingsFound: 0 },
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
        return items.sort((a, b) => (a.price || 0) - (b.price || 0));
      case 'price-desc':
        return items.sort((a, b) => (b.price || 0) - (a.price || 0));
      case 'mileage-asc':
        return items.sort((a, b) => (a.mileage || 0) - (b.mileage || 0));
      case 'mileage-desc':
        return items.sort((a, b) => (b.mileage || 0) - (a.mileage || 0));
      case 'date-desc':
        return items.sort((a, b) => new Date(b.scrapedAt).getTime() - new Date(a.scrapedAt).getTime());
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

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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

    this.isSearching.set(true);
    this.searchProgress.set(0);
    this.loadingMessage.set('Creating scraping job...');
    this.resetScraperStatuses();
    this.results.set([]);

    const year = parseInt(this.selectedYear(), 10);
    const request: CreateScrapingJobRequest = {
      make: this.selectedMake(),
      model: this.selectedModel(),
      yearFrom: isNaN(year) ? undefined : year,
      yearTo: isNaN(year) ? undefined : year,
      sources: Object.values(ScrapingSource),
      maxResults: 500,
    };

    this.scrapingService.createScrapingJob(request)
      .pipe(
        takeUntil(this.destroy$),
        catchError(error => {
          console.error('Failed to create scraping job:', error);
          this.snackBar.open('Failed to create scraping job. Please try again.', 'Close', { duration: 5000 });
          this.isSearching.set(false);
          return of(null);
        })
      )
      .subscribe(session => {
        if (session) {
          this.currentSessionId.set(session.id);
          this.loadingMessage.set('Scraping job created. Starting scrapers...');
          this.pollSessionStatus(session.id);
        }
      });
  }

  private pollSessionStatus(sessionId: string): void {
    const pollingInterval = 2000; // Poll every 2 seconds

    interval(pollingInterval)
      .pipe(
        takeUntil(this.destroy$),
        switchMap(() => this.scrapingService.getSession(sessionId)),
        tap(session => this.updateSessionProgress(session)),
        filter(session => this.isSessionComplete(session)),
        switchMap(() => {
          // Session complete - fetch the listings
          this.loadingMessage.set('Fetching results...');
          return this.scrapingService.searchListings({
            make: this.selectedMake(),
            model: this.selectedModel(),
            yearFrom: parseInt(this.selectedYear(), 10) || undefined,
            yearTo: parseInt(this.selectedYear(), 10) || undefined,
            pageSize: 500,
            sortBy: 'price',
            sortDirection: 'asc',
          });
        }),
        catchError(error => {
          console.error('Error during scraping:', error);
          this.snackBar.open('An error occurred during scraping.', 'Close', { duration: 5000 });
          this.isSearching.set(false);
          return of(null);
        })
      )
      .subscribe(listingsResponse => {
        if (listingsResponse) {
          this.results.set(listingsResponse.items);
          this.totalListingsFound.set(listingsResponse.totalCount);
          this.hasSearched.set(true);
          this.isSearching.set(false);
          this.loadingMessage.set('');

          if (listingsResponse.items.length === 0) {
            this.snackBar.open('No listings found matching your criteria.', 'Close', { duration: 3000 });
          }
        }
      });

    // Also poll for individual job statuses
    this.pollJobStatuses(sessionId);
  }

  private pollJobStatuses(sessionId: string): void {
    const pollingInterval = 1500;

    interval(pollingInterval)
      .pipe(
        takeUntil(this.destroy$),
        filter(() => this.isSearching()),
        switchMap(() => this.scrapingService.getSessionJobs(sessionId)),
        catchError(() => of([]))
      )
      .subscribe(jobs => {
        this.updateJobStatuses(jobs);
      });
  }

  private updateSessionProgress(session: ScrapingSessionResponse): void {
    const statusMap: Record<SearchSessionStatus, string> = {
      [SearchSessionStatus.Pending]: 'Waiting to start...',
      [SearchSessionStatus.Running]: `Scraping in progress... (${session.totalListingsFound} listings found)`,
      [SearchSessionStatus.Completed]: 'Scraping completed!',
      [SearchSessionStatus.Failed]: 'Scraping failed',
      [SearchSessionStatus.Cancelled]: 'Scraping cancelled',
      [SearchSessionStatus.Paused]: 'Scraping paused',
    };

    this.loadingMessage.set(statusMap[session.status] || 'Processing...');
    this.totalListingsFound.set(session.totalListingsFound);

    // Estimate progress based on status
    if (session.status === SearchSessionStatus.Pending) {
      this.searchProgress.set(5);
    } else if (session.status === SearchSessionStatus.Running) {
      // Progress based on listings found (rough estimate)
      const estimatedProgress = Math.min(90, 10 + (session.totalListingsFound / 5));
      this.searchProgress.set(estimatedProgress);
    } else if (session.status === SearchSessionStatus.Completed) {
      this.searchProgress.set(100);
    }
  }

  private updateJobStatuses(jobs: ScrapingJobResponse[]): void {
    this.scraperStatuses.update(statuses =>
      statuses.map(scraper => {
        const job = jobs.find(j => j.source === scraper.id);
        if (!job) return scraper;

        let status: ScraperStatus['status'] = 'pending';
        if (job.status === ScrapingJobStatus.Running) {
          status = 'running';
        } else if (job.status === ScrapingJobStatus.Completed) {
          status = 'done';
        } else if (job.status === ScrapingJobStatus.Failed) {
          status = 'error';
        }

        return {
          ...scraper,
          status,
          listingsFound: job.listingsFound,
        };
      })
    );

    // Update progress based on completed jobs
    const completedJobs = jobs.filter(j =>
      j.status === ScrapingJobStatus.Completed || j.status === ScrapingJobStatus.Failed
    ).length;
    const totalJobs = jobs.length || 1;
    const jobProgress = (completedJobs / totalJobs) * 90 + 5;
    this.searchProgress.set(Math.min(95, jobProgress));
  }

  private isSessionComplete(session: ScrapingSessionResponse): boolean {
    return session.status === SearchSessionStatus.Completed ||
           session.status === SearchSessionStatus.Failed ||
           session.status === SearchSessionStatus.Cancelled;
  }

  private resetScraperStatuses(): void {
    this.scraperStatuses.update(statuses =>
      statuses.map(s => ({ ...s, status: 'pending' as const, listingsFound: 0 }))
    );
  }

  protected formatPrice(price: number | undefined): string {
    if (!price) return 'N/A';
    return '$' + price.toLocaleString();
  }

  protected formatMileage(mileage: number | undefined): string {
    if (!mileage) return 'N/A';
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

  protected getScraperStatusClass(status: ScraperStatus['status']): string {
    switch (status) {
      case 'pending': return 'scraper-chip--pending';
      case 'running': return 'scraper-chip--running';
      case 'done': return 'scraper-chip--done';
      case 'error': return 'scraper-chip--error';
      default: return '';
    }
  }

  protected onExport(): void {
    const currentResults = this.results();
    if (currentResults.length === 0) {
      this.snackBar.open('No results to export.', 'Close', { duration: 3000 });
      return;
    }

    const csv = this.convertToCSV(currentResults);
    this.downloadCSV(csv, 'car-search-results.csv');
  }

  private convertToCSV(results: CarListing[]): string {
    const headers = ['Title', 'Make', 'Model', 'Year', 'Price', 'Mileage', 'Location', 'Source', 'Transmission', 'Drivetrain', 'Fuel Type', 'VIN', 'URL', 'Scraped At'];
    const rows = results.map(r => [
      r.title || '',
      r.make || '',
      r.model || '',
      r.year?.toString() || '',
      r.price?.toString() || '',
      r.mileage?.toString() || '',
      r.location || `${r.city || ''}, ${r.province || ''}`.trim(),
      r.sourceSite || '',
      r.transmission || '',
      r.drivetrain || '',
      r.fuelType || '',
      r.vin || '',
      r.listingUrl || '',
      r.scrapedAt ? new Date(r.scrapedAt).toLocaleDateString() : '',
    ]);

    return [headers, ...rows].map(row => row.map(cell => `"${cell.replace(/"/g, '""')}"`).join(',')).join('\n');
  }

  private downloadCSV(csv: string, filename: string): void {
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
  }

  protected openListing(listing: CarListing): void {
    if (listing.listingUrl) {
      window.open(listing.listingUrl, '_blank');
    }
  }

  protected onCancelSearch(): void {
    const sessionId = this.currentSessionId();
    if (sessionId) {
      this.scrapingService.cancelSession(sessionId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.snackBar.open('Search cancelled.', 'Close', { duration: 3000 });
            this.isSearching.set(false);
          },
          error: () => {
            this.snackBar.open('Failed to cancel search.', 'Close', { duration: 3000 });
          }
        });
    } else {
      this.isSearching.set(false);
    }
  }

  protected getListingImage(listing: CarListing): string | null {
    return listing.imageUrls && listing.imageUrls.length > 0 ? listing.imageUrls[0] : null;
  }
}
