import { Component, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

interface ApiKey {
  id: string;
  name: string;
  keyPrefix: string;
  ownerId: string;
  ownerName: string;
  ownerInitials: string;
  status: 'active' | 'revoked' | 'expired';
  expiresAt: string | null;
  lastUsedAt: string;
}

interface ApiKeyStats {
  total: number;
  active: number;
  revoked: number;
  expired: number;
}

@Component({
  selector: 'app-apikey-list',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatMenuModule,
    MatPaginatorModule,
  ],
  templateUrl: './apikey-list.html',
  styleUrl: './apikey-list.scss',
})
export class ApiKeyListComponent {
  protected readonly displayedColumns = [
    'name',
    'keyPrefix',
    'owner',
    'status',
    'expires',
    'lastUsed',
    'actions',
  ];

  protected readonly stats = signal<ApiKeyStats>({
    total: 24,
    active: 18,
    revoked: 4,
    expired: 2,
  });

  protected readonly searchQuery = signal('');
  protected readonly statusFilter = signal<string>('all');
  protected readonly ownerFilter = signal<string>('all');

  protected readonly apiKeys = signal<ApiKey[]>([
    {
      id: '1',
      name: 'Production API',
      keyPrefix: 'aK3bF7xZ...',
      ownerId: '1',
      ownerName: 'John Smith',
      ownerInitials: 'JS',
      status: 'active',
      expiresAt: null,
      lastUsedAt: '2 hours ago',
    },
    {
      id: '2',
      name: 'Development Key',
      keyPrefix: 'mX9kL2pQ...',
      ownerId: '2',
      ownerName: 'Sarah Anderson',
      ownerInitials: 'SA',
      status: 'active',
      expiresAt: 'Mar 15, 2026',
      lastUsedAt: '5 hours ago',
    },
    {
      id: '3',
      name: 'Data Import Service',
      keyPrefix: 'rT5nW8vY...',
      ownerId: '3',
      ownerName: 'Michael Chen',
      ownerInitials: 'MC',
      status: 'active',
      expiresAt: 'Jun 30, 2026',
      lastUsedAt: '1 day ago',
    },
    {
      id: '4',
      name: 'Old Integration Key',
      keyPrefix: 'bH6jC4dE...',
      ownerId: '1',
      ownerName: 'John Smith',
      ownerInitials: 'JS',
      status: 'revoked',
      expiresAt: null,
      lastUsedAt: '30 days ago',
    },
    {
      id: '5',
      name: 'Temp Access Key',
      keyPrefix: 'zP1qR3sT...',
      ownerId: '4',
      ownerName: 'Emily Watson',
      ownerInitials: 'EW',
      status: 'expired',
      expiresAt: 'Jan 1, 2026',
      lastUsedAt: '25 days ago',
    },
  ]);

  protected readonly pageSize = signal(10);
  protected readonly pageIndex = signal(0);
  protected readonly totalItems = computed(() => this.stats().total);

  protected readonly filteredApiKeys = computed(() => {
    let result = this.apiKeys();
    const query = this.searchQuery().toLowerCase();
    const status = this.statusFilter();
    const owner = this.ownerFilter();

    if (query) {
      result = result.filter(
        (k) =>
          k.name.toLowerCase().includes(query) ||
          k.keyPrefix.toLowerCase().includes(query)
      );
    }

    if (status !== 'all') {
      result = result.filter((k) => k.status === status);
    }

    if (owner !== 'all') {
      result = result.filter((k) => k.ownerId === owner);
    }

    return result;
  });

  protected onSearchChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  protected onStatusFilterChange(value: string): void {
    this.statusFilter.set(value);
  }

  protected onOwnerFilterChange(value: string): void {
    this.ownerFilter.set(value);
  }

  protected onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  protected getStatusClass(status: string): string {
    switch (status) {
      case 'active':
        return 'chip--active';
      case 'revoked':
        return 'chip--revoked';
      case 'expired':
        return 'chip--expired';
      default:
        return '';
    }
  }

  protected getStatusLabel(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  protected onRevoke(keyId: string): void {
    console.log('Revoke key:', keyId);
  }

  protected onRegenerate(keyId: string): void {
    console.log('Regenerate key:', keyId);
  }
}
