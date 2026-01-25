import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Enums matching backend
export enum ScrapingSource {
  Auto123 = 'Auto123',
  Autotrader = 'Autotrader',
  CarFax = 'CarFax',
  CarGurus = 'CarGurus',
  CarMax = 'CarMax',
  Carvana = 'Carvana',
  Clutch = 'Clutch',
  Kijiji = 'Kijiji',
  TabangiMotors = 'TabangiMotors',
  TrueCar = 'TrueCar',
  Vroom = 'Vroom',
}

export enum SearchSessionStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
  Paused = 'Paused',
}

export enum ScrapingJobStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed',
  Retry = 'Retry',
}

// Request DTOs
export interface CreateScrapingJobRequest {
  make?: string;
  model?: string;
  yearFrom?: number;
  yearTo?: number;
  minPrice?: number;
  maxPrice?: number;
  maxMileage?: number;
  postalCode?: string;
  radiusKm?: number;
  province?: string;
  sources: ScrapingSource[];
  maxResults?: number;
}

// Response DTOs
export interface SearchParametersResponse {
  make?: string;
  model?: string;
  yearFrom?: number;
  yearTo?: number;
  minPrice?: number;
  maxPrice?: number;
  maxMileage?: number;
  postalCode?: string;
  radiusKm?: number;
  province?: string;
  maxResults?: number;
}

export interface ScrapingSessionResponse {
  id: string;
  status: SearchSessionStatus;
  sources: ScrapingSource[];
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  totalListingsFound: number;
  totalErrors: number;
  errorMessage?: string;
  parameters: SearchParametersResponse;
}

export interface ScrapingJobResponse {
  id: string;
  sessionId: string;
  source: ScrapingSource;
  status: ScrapingJobStatus;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  listingsFound: number;
  pagesCrawled: number;
  errorMessage?: string;
  retryCount: number;
}

export interface SourceHealthStatus {
  isHealthy: boolean;
  successRate: number;
  lastSuccessAt?: string;
  lastFailureAt?: string;
}

export interface ScrapingHealthResponse {
  status: string;
  activeSessions: number;
  pendingJobs: number;
  runningJobs: number;
  sourceStatuses: Record<string, SourceHealthStatus>;
}

// Listing DTOs (from Listing Service)
export interface CarListing {
  id: string;
  externalId: string;
  sourceSite: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage?: number;
  location?: string;
  province?: string;
  city?: string;
  transmission?: string;
  drivetrain?: string;
  fuelType?: string;
  exteriorColor?: string;
  bodyStyle?: string;
  vin?: string;
  listingUrl: string;
  imageUrls: string[];
  description?: string;
  dealerName?: string;
  isDealer: boolean;
  scrapedAt: string;
  firstSeenAt: string;
  lastSeenAt: string;
}

export interface ListingSearchResponse {
  items: CarListing[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root',
})
export class ScrapingService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api';

  // Scraping Orchestration endpoints
  createScrapingJob(request: CreateScrapingJobRequest): Observable<ScrapingSessionResponse> {
    return this.http.post<ScrapingSessionResponse>(
      `${this.apiBaseUrl}/scraping/jobs`,
      request
    );
  }

  getSession(sessionId: string): Observable<ScrapingSessionResponse> {
    return this.http.get<ScrapingSessionResponse>(
      `${this.apiBaseUrl}/scraping/jobs/${sessionId}`
    );
  }

  getSessionJobs(sessionId: string): Observable<ScrapingJobResponse[]> {
    return this.http.get<ScrapingJobResponse[]>(
      `${this.apiBaseUrl}/scraping/sessions/${sessionId}/jobs`
    );
  }

  cancelSession(sessionId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiBaseUrl}/scraping/jobs/${sessionId}/cancel`,
      {}
    );
  }

  getHealth(): Observable<ScrapingHealthResponse> {
    return this.http.get<ScrapingHealthResponse>(
      `${this.apiBaseUrl}/scraping/health`
    );
  }

  // Listing Service endpoints
  searchListings(params: {
    make?: string;
    model?: string;
    yearFrom?: number;
    yearTo?: number;
    minPrice?: number;
    maxPrice?: number;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
  }): Observable<ListingSearchResponse> {
    const queryParams = new URLSearchParams();

    if (params.make) queryParams.set('make', params.make);
    if (params.model) queryParams.set('model', params.model);
    if (params.yearFrom) queryParams.set('yearFrom', params.yearFrom.toString());
    if (params.yearTo) queryParams.set('yearTo', params.yearTo.toString());
    if (params.minPrice) queryParams.set('minPrice', params.minPrice.toString());
    if (params.maxPrice) queryParams.set('maxPrice', params.maxPrice.toString());
    if (params.page) queryParams.set('page', params.page.toString());
    if (params.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params.sortBy) queryParams.set('sortBy', params.sortBy);
    if (params.sortDirection) queryParams.set('sortDirection', params.sortDirection);

    return this.http.get<ListingSearchResponse>(
      `${this.apiBaseUrl}/listings?${queryParams.toString()}`
    );
  }

  getListingsBySession(sessionId: string): Observable<CarListing[]> {
    return this.http.get<CarListing[]>(
      `${this.apiBaseUrl}/listings/session/${sessionId}`
    );
  }
}
