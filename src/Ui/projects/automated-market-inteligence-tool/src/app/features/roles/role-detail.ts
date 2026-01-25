import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { FormsModule } from '@angular/forms';

interface Permission {
  id: string;
  label: string;
  checked: boolean;
}

interface PermissionGroup {
  name: string;
  icon: string;
  permissions: Permission[];
}

interface AssignedUser {
  id: string;
  displayName: string;
  email: string;
  initials: string;
}

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatDividerModule,
    FormsModule,
  ],
  templateUrl: './role-detail.html',
  styleUrl: './role-detail.scss',
})
export class RoleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);

  protected readonly isNew = signal(false);
  protected readonly roleId = signal<string | null>(null);

  protected readonly role = signal({
    id: '4',
    name: 'Market Analyst',
    description:
      'Access to market analysis tools and reports. Can create and manage intelligence reports.',
    icon: 'analytics',
    isSystem: false,
    createdAt: 'January 10, 2026',
  });

  protected readonly permissionGroups = signal<PermissionGroup[]>([
    {
      name: 'Users',
      icon: 'people',
      permissions: [
        { id: 'users-view', label: 'View', checked: true },
        { id: 'users-create', label: 'Create', checked: false },
        { id: 'users-edit', label: 'Edit', checked: false },
        { id: 'users-delete', label: 'Delete', checked: false },
      ],
    },
    {
      name: 'Reports',
      icon: 'assessment',
      permissions: [
        { id: 'reports-view', label: 'View', checked: true },
        { id: 'reports-create', label: 'Create', checked: true },
        { id: 'reports-edit', label: 'Edit', checked: true },
        { id: 'reports-delete', label: 'Delete', checked: true },
        { id: 'reports-export', label: 'Export', checked: true },
      ],
    },
    {
      name: 'Market Data',
      icon: 'trending_up',
      permissions: [
        { id: 'market-view', label: 'View', checked: true },
        { id: 'market-analyze', label: 'Analyze', checked: true },
        { id: 'market-import', label: 'Import', checked: false },
        { id: 'market-export', label: 'Export', checked: true },
      ],
    },
    {
      name: 'Dashboards',
      icon: 'dashboard',
      permissions: [
        { id: 'dash-view', label: 'View', checked: true },
        { id: 'dash-create', label: 'Create', checked: true },
        { id: 'dash-edit', label: 'Edit', checked: true },
        { id: 'dash-delete', label: 'Delete', checked: false },
        { id: 'dash-share', label: 'Share', checked: true },
      ],
    },
    {
      name: 'Settings',
      icon: 'settings',
      permissions: [
        { id: 'settings-view', label: 'View', checked: false },
        { id: 'settings-edit', label: 'Edit', checked: false },
      ],
    },
    {
      name: 'API Keys',
      icon: 'vpn_key',
      permissions: [
        { id: 'api-view', label: 'View', checked: false },
        { id: 'api-create', label: 'Create', checked: false },
        { id: 'api-revoke', label: 'Revoke', checked: false },
      ],
    },
  ]);

  protected readonly assignedUsers = signal<AssignedUser[]>([
    { id: '1', displayName: 'John Doe', email: 'john.doe@example.com', initials: 'JD' },
    { id: '2', displayName: 'Alice Smith', email: 'alice.smith@example.com', initials: 'AS' },
    { id: '3', displayName: 'Bob Wilson', email: 'bob.wilson@example.com', initials: 'BW' },
    { id: '4', displayName: 'Carol Martinez', email: 'carol.martinez@example.com', initials: 'CM' },
    { id: '5', displayName: 'David Lee', email: 'david.lee@example.com', initials: 'DL' },
  ]);

  protected readonly totalAssignedUsers = signal(23);

  protected readonly pageTitle = computed(() =>
    this.isNew() ? 'Create Role' : this.role().name
  );

  protected readonly canEdit = computed(() => !this.role().isSystem);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id === 'new' || !id) {
      this.isNew.set(true);
      this.role.set({
        id: '',
        name: '',
        description: '',
        icon: 'shield',
        isSystem: false,
        createdAt: new Date().toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'long',
          day: 'numeric',
        }),
      });
      // Reset all permissions for new role
      this.permissionGroups.update((groups) =>
        groups.map((g) => ({
          ...g,
          permissions: g.permissions.map((p) => ({ ...p, checked: false })),
        }))
      );
      this.assignedUsers.set([]);
      this.totalAssignedUsers.set(0);
    } else {
      this.roleId.set(id);
      // In real app, load role data from API
    }
  }

  protected onNameChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.role.update((r) => ({ ...r, name: input.value }));
  }

  protected onDescriptionChange(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    this.role.update((r) => ({ ...r, description: input.value }));
  }

  protected onPermissionChange(groupIndex: number, permIndex: number, checked: boolean): void {
    this.permissionGroups.update((groups) =>
      groups.map((g, gi) =>
        gi === groupIndex
          ? {
              ...g,
              permissions: g.permissions.map((p, pi) =>
                pi === permIndex ? { ...p, checked } : p
              ),
            }
          : g
      )
    );
  }

  protected onSave(): void {
    console.log('Saving role:', this.role());
    console.log('Permissions:', this.permissionGroups());
  }

  protected onClone(): void {
    console.log('Clone role:', this.role().id);
  }

  protected onDelete(): void {
    console.log('Delete role:', this.role().id);
  }
}
