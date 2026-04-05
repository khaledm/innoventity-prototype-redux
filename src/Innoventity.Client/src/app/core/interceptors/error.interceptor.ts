import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Global error handling
      if (error.status === 401) {
        // Unauthorized - clear tokens and redirect
        authService.logout();
        router.navigate(['/login'], { queryParams: { sessionExpired: 'true' } });
      } else if (error.status === 403) {
        // Forbidden - show error page
        router.navigate(['/forbidden']);
      } else if (error.status === 500) {
        // Server error - log to console (production: send to Application Insights)
        console.error('Server error:', error);
      }

      // Re-throw for component-specific handling
      return throwError(() => error);
    })
  );
};
