import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'car-search',
    pathMatch: 'full',
  },
  {
    path: 'car-search',
    loadComponent: () =>
      import('./features/car-search/car-search').then((m) => m.CarSearchComponent),
  },
  {
    path: 'users',
    loadComponent: () =>
      import('./features/users/user-list').then((m) => m.UserListComponent),
  },
  {
    path: 'users/new',
    loadComponent: () =>
      import('./features/users/user-detail').then((m) => m.UserDetailComponent),
  },
  {
    path: 'users/:id',
    loadComponent: () =>
      import('./features/users/user-detail').then((m) => m.UserDetailComponent),
  },
  {
    path: 'roles',
    loadComponent: () =>
      import('./features/roles/role-list').then((m) => m.RoleListComponent),
  },
  {
    path: 'roles/new',
    loadComponent: () =>
      import('./features/roles/role-detail').then((m) => m.RoleDetailComponent),
  },
  {
    path: 'roles/:id',
    loadComponent: () =>
      import('./features/roles/role-detail').then((m) => m.RoleDetailComponent),
  },
  {
    path: 'api-keys',
    loadComponent: () =>
      import('./features/api-keys/apikey-list').then((m) => m.ApiKeyListComponent),
  },
  {
    path: 'api-keys/new',
    loadComponent: () =>
      import('./features/api-keys/apikey-detail').then((m) => m.ApiKeyDetailComponent),
  },
  {
    path: 'api-keys/:id',
    loadComponent: () =>
      import('./features/api-keys/apikey-detail').then((m) => m.ApiKeyDetailComponent),
  },
];
