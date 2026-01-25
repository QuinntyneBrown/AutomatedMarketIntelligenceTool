import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { FormsModule } from '@angular/forms';

interface Role {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
  assigned: boolean;
}

interface ActivityItem {
  icon: string;
  title: string;
  description: string;
  time: string;
}

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatCheckboxModule,
    MatDividerModule,
    FormsModule,
  ],
  templateUrl: './user-detail.html',
  styleUrl: './user-detail.scss',
})
export class UserDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);

  protected readonly isNew = signal(false);
  protected readonly userId = signal<string | null>(null);

  protected readonly user = signal({
    id: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    email: 'john.smith@example.com',
    displayName: 'John Smith',
    initials: 'JS',
    isEmailVerified: true,
    isActive: true,
    createdAt: 'January 15, 2025',
    lastLoginAt: 'January 25, 2026 at 2:30 PM',
    failedLoginAttempts: 0,
    lockoutEndAt: null as string | null,
  });

  protected readonly roles = signal<Role[]>([
    {
      id: '1',
      name: 'Admin',
      description: 'Full system access with administrative privileges',
      isSystem: true,
      assigned: true,
    },
    {
      id: '2',
      name: 'User',
      description: 'Standard user access for day-to-day operations',
      isSystem: true,
      assigned: true,
    },
    {
      id: '3',
      name: 'ReadOnly',
      description: 'View-only access to reports and dashboards',
      isSystem: true,
      assigned: false,
    },
  ]);

  protected readonly activities = signal<ActivityItem[]>([
    {
      icon: 'login',
      title: 'Successful Login',
      description: 'Logged in from 192.168.1.105',
      time: '2 hours ago',
    },
    {
      icon: 'edit',
      title: 'Profile Updated',
      description: 'Changed display name',
      time: '1 day ago',
    },
    {
      icon: 'security',
      title: 'Password Changed',
      description: 'Password was reset successfully',
      time: '5 days ago',
    },
    {
      icon: 'person_add',
      title: 'Account Created',
      description: 'User account was created by admin',
      time: '10 days ago',
    },
  ]);

  protected readonly pageTitle = computed(() =>
    this.isNew() ? 'Create User' : this.user().displayName
  );

  protected readonly lockoutStatus = computed(() => {
    const user = this.user();
    if (user.lockoutEndAt) {
      return { text: 'Locked', class: 'info-item__value--error' };
    }
    return { text: 'Not Locked', class: 'info-item__value--success' };
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id === 'new' || !id) {
      this.isNew.set(true);
      this.user.set({
        id: '',
        email: '',
        displayName: '',
        initials: '',
        isEmailVerified: false,
        isActive: true,
        createdAt: new Date().toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'long',
          day: 'numeric',
        }),
        lastLoginAt: 'Never',
        failedLoginAttempts: 0,
        lockoutEndAt: null,
      });
    } else {
      this.userId.set(id);
      // In real app, load user data from API
    }
  }

  protected onEmailChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.user.update((u) => ({ ...u, email: input.value }));
  }

  protected onDisplayNameChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const displayName = input.value;
    const initials = this.getInitials(displayName);
    this.user.update((u) => ({ ...u, displayName, initials }));
  }

  protected onEmailVerifiedChange(checked: boolean): void {
    this.user.update((u) => ({ ...u, isEmailVerified: checked }));
  }

  protected onActiveChange(checked: boolean): void {
    this.user.update((u) => ({ ...u, isActive: checked }));
  }

  protected onRoleToggle(roleId: string): void {
    this.roles.update((roles) =>
      roles.map((r) => (r.id === roleId ? { ...r, assigned: !r.assigned } : r))
    );
  }

  protected onSave(): void {
    console.log('Saving user:', this.user());
    console.log('Assigned roles:', this.roles().filter((r) => r.assigned));
  }

  protected onResetPassword(): void {
    console.log('Reset password for user:', this.user().id);
  }

  protected onDeactivate(): void {
    console.log('Deactivate user:', this.user().id);
  }

  protected onDelete(): void {
    console.log('Delete user:', this.user().id);
  }

  private getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }
}
