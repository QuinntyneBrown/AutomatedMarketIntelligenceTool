import { Component, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';

interface Role {
  id: string;
  name: string;
  description: string;
  icon: string;
  isSystem: boolean;
  userCount: number;
  createdAt: string;
}

interface RoleStats {
  total: number;
  system: number;
  custom: number;
}

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatMenuModule,
  ],
  templateUrl: './role-list.html',
  styleUrl: './role-list.scss',
})
export class RoleListComponent {
  protected readonly stats = signal<RoleStats>({
    total: 6,
    system: 3,
    custom: 3,
  });

  protected readonly searchQuery = signal('');
  protected readonly typeFilter = signal<string>('all');

  protected readonly roles = signal<Role[]>([
    {
      id: '1',
      name: 'Admin',
      description:
        'Full system access with administrative privileges. Can manage users, roles, and system settings.',
      icon: 'security',
      isSystem: true,
      userCount: 12,
      createdAt: 'System Default',
    },
    {
      id: '2',
      name: 'User',
      description: 'Standard user access for day-to-day operations. Can view and manage own data.',
      icon: 'person',
      isSystem: true,
      userCount: 185,
      createdAt: 'System Default',
    },
    {
      id: '3',
      name: 'ReadOnly',
      description: 'View-only access to reports and dashboards. Cannot modify any data.',
      icon: 'visibility',
      isSystem: true,
      userCount: 45,
      createdAt: 'System Default',
    },
    {
      id: '4',
      name: 'Market Analyst',
      description:
        'Access to market analysis tools and reports. Can create and manage intelligence reports.',
      icon: 'analytics',
      isSystem: false,
      userCount: 23,
      createdAt: 'Jan 10, 2026',
    },
    {
      id: '5',
      name: 'Data Steward',
      description: 'Manages data quality and integrity. Can approve and modify data imports.',
      icon: 'storage',
      isSystem: false,
      userCount: 8,
      createdAt: 'Jan 15, 2026',
    },
    {
      id: '6',
      name: 'API Integration',
      description: 'Service accounts for API integrations. Limited to programmatic access only.',
      icon: 'api',
      isSystem: false,
      userCount: 5,
      createdAt: 'Jan 20, 2026',
    },
  ]);

  protected readonly filteredRoles = computed(() => {
    let result = this.roles();
    const query = this.searchQuery().toLowerCase();
    const type = this.typeFilter();

    if (query) {
      result = result.filter((r) => r.name.toLowerCase().includes(query));
    }

    if (type === 'system') {
      result = result.filter((r) => r.isSystem);
    } else if (type === 'custom') {
      result = result.filter((r) => !r.isSystem);
    }

    return result;
  });

  protected onSearchChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  protected onTypeFilterChange(value: string): void {
    this.typeFilter.set(value);
  }

  protected onCloneRole(roleId: string): void {
    console.log('Clone role:', roleId);
  }

  protected onDeleteRole(roleId: string): void {
    console.log('Delete role:', roleId);
  }
}
