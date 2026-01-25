import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent {
  protected readonly sidenavOpened = signal(true);

  protected readonly navItems = signal<NavItem[]>([
    { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
    { label: 'Car Search', icon: 'search', route: '/car-search' },
    { label: 'User Management', icon: 'people', route: '/users' },
    { label: 'Role Management', icon: 'admin_panel_settings', route: '/roles' },
    { label: 'API Keys', icon: 'vpn_key', route: '/api-keys' },
  ]);

  protected toggleSidenav(): void {
    this.sidenavOpened.update((v) => !v);
  }
}
