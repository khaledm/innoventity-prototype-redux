import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  // Fallback to localStorage in case the signal isn't populated yet (e.g. on a fresh page load
  // before Angular's DI has fully initialised the AuthService singleton).
  const token = authService.accessToken() ??
    (typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null);

  // Don't add auth header for auth endpoints
  if (token && !req.url.includes('/auth/')) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req);
};
