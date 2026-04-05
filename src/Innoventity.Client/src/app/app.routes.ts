import { Routes } from '@angular/router';
import { AuthGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },

  // Auth routes (eager-loaded for critical path)
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  // TODO: Add register route when implemented in future task
  // TODO: Add innovation routes when implemented in T073

  // Fallback
  { path: '**', redirectTo: '/login' }
];
