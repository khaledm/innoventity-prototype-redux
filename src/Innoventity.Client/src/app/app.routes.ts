import { Routes } from '@angular/router';
import { AuthGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },

  // Auth routes (eager-loaded for critical path)
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },

  // Innovation routes (protected with AuthGuard)
  {
    path: 'innovations',
    loadComponent: () => import('./features/innovations/innovations-list/innovations-list.component').then(m => m.InnovationsListComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'innovations/:id',
    loadComponent: () => import('./features/innovations/innovation-detail/innovation-detail.component').then(m => m.InnovationDetailComponent),
    canActivate: [AuthGuard]
  },

  // Fallback
  { path: '**', redirectTo: '/login' }
];
