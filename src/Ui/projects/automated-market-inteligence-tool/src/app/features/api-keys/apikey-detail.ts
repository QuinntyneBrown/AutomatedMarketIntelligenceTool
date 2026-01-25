import { Component, signal, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Clipboard } from '@angular/cdk/clipboard';

interface ApiKeyDetail {
  id: string;
  name: string;
  keyPrefix: string;
  fullKey: string;
  ownerId: string;
  ownerName: string;
  status: 'active' | 'revoked' | 'expired';
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string;
  scopes: string[];
}

interface UsageStats {
  totalRequests: number;
  successRate: number;
  lastUsed: string;
  avgRequestsPerDay: number;
}

interface ActivityItem {
  id: string;
  method: string;
  endpoint: string;
  statusCode: number;
  statusText: string;
  duration: string;
  ip: string;
  timestamp: string;
  success: boolean;
}

interface Scope {
  id: string;
  label: string;
  selected: boolean;
}

@Component({
  selector: 'app-apikey-detail',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
  ],
  templateUrl: './apikey-detail.html',
  styleUrl: './apikey-detail.scss',
})
export class ApiKeyDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly clipboard = inject(Clipboard);

  protected readonly keyId = signal<string | null>(null);
  protected readonly isNew = computed(() => this.keyId() === 'new' || !this.keyId());
  protected readonly showFullKey = signal(false);

  protected readonly apiKey = signal<ApiKeyDetail>({
    id: '1',
    name: 'Production API',
    keyPrefix: 'aK3bF7xZ',
    fullKey: 'aK3bF7xZ9mP2qR5tU8wY1cV4bN6hJ0kL3oI7eD2sA5fG8hJ',
    ownerId: '1',
    ownerName: 'John Smith',
    status: 'active',
    createdAt: 'January 15, 2026',
    expiresAt: null,
    lastUsedAt: '2 hours ago',
    scopes: ['read:reports', 'write:reports', 'read:market-data', 'analyze:market-data'],
  });

  protected readonly usageStats = signal<UsageStats>({
    totalRequests: 12847,
    successRate: 99.2,
    lastUsed: '2 hours ago',
    avgRequestsPerDay: 428,
  });

  protected readonly allScopes = signal<Scope[]>([
    { id: 'read:reports', label: 'read:reports', selected: true },
    { id: 'write:reports', label: 'write:reports', selected: true },
    { id: 'read:market-data', label: 'read:market-data', selected: true },
    { id: 'analyze:market-data', label: 'analyze:market-data', selected: true },
    { id: 'read:users', label: 'read:users', selected: false },
    { id: 'write:users', label: 'write:users', selected: false },
    { id: 'admin:all', label: 'admin:all', selected: false },
  ]);

  protected readonly recentActivity = signal<ActivityItem[]>([
    {
      id: '1',
      method: 'GET',
      endpoint: '/api/reports',
      statusCode: 200,
      statusText: 'OK',
      duration: '145ms',
      ip: '192.168.1.105',
      timestamp: '2 hours ago',
      success: true,
    },
    {
      id: '2',
      method: 'POST',
      endpoint: '/api/market-data/analyze',
      statusCode: 200,
      statusText: 'OK',
      duration: '892ms',
      ip: '192.168.1.105',
      timestamp: '2 hours ago',
      success: true,
    },
    {
      id: '3',
      method: 'GET',
      endpoint: '/api/market-data',
      statusCode: 200,
      statusText: 'OK',
      duration: '234ms',
      ip: '192.168.1.105',
      timestamp: '3 hours ago',
      success: true,
    },
    {
      id: '4',
      method: 'POST',
      endpoint: '/api/users',
      statusCode: 403,
      statusText: 'Forbidden - Scope not authorized',
      duration: '',
      ip: '192.168.1.105',
      timestamp: '5 hours ago',
      success: false,
    },
    {
      id: '5',
      method: 'GET',
      endpoint: '/api/reports/export',
      statusCode: 200,
      statusText: 'OK',
      duration: '1,245ms',
      ip: '192.168.1.105',
      timestamp: '6 hours ago',
      success: true,
    },
  ]);

  protected readonly pageTitle = computed(() => {
    if (this.isNew()) {
      return 'Generate New API Key';
    }
    return this.apiKey().name;
  });

  protected readonly displayedKey = computed(() => {
    if (this.showFullKey()) {
      return this.apiKey().fullKey;
    }
    return this.apiKey().fullKey.substring(0, 32) + '...';
  });

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      this.keyId.set(id);
    });
  }

  protected onCopyKey(): void {
    this.clipboard.copy(this.apiKey().fullKey);
  }

  protected onCopyPrefix(): void {
    this.clipboard.copy(this.apiKey().keyPrefix);
  }

  protected onToggleKeyVisibility(): void {
    this.showFullKey.update((v) => !v);
  }

  protected onNameChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.apiKey.update((key) => ({ ...key, name: input.value }));
  }

  protected onScopeToggle(scopeId: string): void {
    this.allScopes.update((scopes) =>
      scopes.map((s) => (s.id === scopeId ? { ...s, selected: !s.selected } : s))
    );
  }

  protected onRegenerate(): void {
    console.log('Regenerate key:', this.apiKey().id);
  }

  protected onRevoke(): void {
    console.log('Revoke key:', this.apiKey().id);
  }

  protected onDelete(): void {
    console.log('Delete key:', this.apiKey().id);
  }

  protected onSave(): void {
    console.log('Save key:', this.apiKey());
  }

  protected formatNumber(num: number): string {
    return num.toLocaleString();
  }
}
