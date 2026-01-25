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

interface User {
  id: string;
  email: string;
  displayName: string;
  initials: string;
  roles: string[];
  status: 'active' | 'inactive' | 'locked';
  lastLogin: string;
}

interface UserStats {
  total: number;
  active: number;
  inactive: number;
  locked: number;
}

@Component({
  selector: 'app-user-list',
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
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss',
})
export class UserListComponent {
  protected readonly displayedColumns = ['user', 'roles', 'status', 'lastLogin', 'actions'];

  protected readonly stats = signal<UserStats>({
    total: 247,
    active: 231,
    inactive: 16,
    locked: 3,
  });

  protected readonly searchQuery = signal('');
  protected readonly statusFilter = signal<string>('all');
  protected readonly roleFilter = signal<string>('all');

  protected readonly users = signal<User[]>([
    {
      id: '1',
      email: 'john.smith@example.com',
      displayName: 'John Smith',
      initials: 'JS',
      roles: ['Admin', 'User'],
      status: 'active',
      lastLogin: '2 hours ago',
    },
    {
      id: '2',
      email: 'sarah.anderson@example.com',
      displayName: 'Sarah Anderson',
      initials: 'SA',
      roles: ['User'],
      status: 'active',
      lastLogin: '5 hours ago',
    },
    {
      id: '3',
      email: 'michael.chen@example.com',
      displayName: 'Michael Chen',
      initials: 'MC',
      roles: ['User'],
      status: 'locked',
      lastLogin: '3 days ago',
    },
    {
      id: '4',
      email: 'emily.watson@example.com',
      displayName: 'Emily Watson',
      initials: 'EW',
      roles: ['ReadOnly'],
      status: 'inactive',
      lastLogin: '1 week ago',
    },
    {
      id: '5',
      email: 'robert.johnson@example.com',
      displayName: 'Robert Johnson',
      initials: 'RJ',
      roles: ['Admin'],
      status: 'active',
      lastLogin: '1 hour ago',
    },
  ]);

  protected readonly pageSize = signal(10);
  protected readonly pageIndex = signal(0);
  protected readonly totalItems = computed(() => this.stats().total);

  protected readonly filteredUsers = computed(() => {
    let result = this.users();
    const query = this.searchQuery().toLowerCase();
    const status = this.statusFilter();
    const role = this.roleFilter();

    if (query) {
      result = result.filter(
        (u) =>
          u.displayName.toLowerCase().includes(query) ||
          u.email.toLowerCase().includes(query)
      );
    }

    if (status !== 'all') {
      result = result.filter((u) => u.status === status);
    }

    if (role !== 'all') {
      result = result.filter((u) => u.roles.includes(role));
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

  protected onRoleFilterChange(value: string): void {
    this.roleFilter.set(value);
  }

  protected onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  protected getStatusClass(status: string): string {
    switch (status) {
      case 'active':
        return 'chip--active';
      case 'inactive':
        return 'chip--inactive';
      case 'locked':
        return 'chip--locked';
      default:
        return '';
    }
  }

  protected getStatusLabel(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }
}
