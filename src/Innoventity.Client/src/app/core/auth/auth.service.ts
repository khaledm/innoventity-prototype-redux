import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { User, LoginRequest, LoginResponse, ActorType } from '../models/user.model';
import { Result } from '../models/result.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Private signals
  private accessTokenSignal = signal<string | null>(
    typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null
  );
  private userSignal = signal<User | null>(null);

  // Public computed signals
  isAuthenticated = computed(() => this.accessTokenSignal() !== null);
  currentUser = computed(() => this.userSignal());
  accessToken = computed(() => this.accessTokenSignal());

  constructor(private router: Router, private http: HttpClient) {
    // Restore user from localStorage if token exists
    if (this.isAuthenticated()) {
      const storedUser = localStorage.getItem('currentUser');
      if (storedUser) {
        this.userSignal.set(JSON.parse(storedUser));
      }
    }
  }

  async login(email: string, password: string, actorType: ActorType): Promise<Result<void>> {
    try {
      const request: LoginRequest = { email, password, actorType };
      const response = await firstValueFrom(
        this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      );

      // Store tokens and user
      this.accessTokenSignal.set(response.accessToken);
      this.userSignal.set(response.actor);
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);
      localStorage.setItem('currentUser', JSON.stringify(response.actor));

      return { success: true };
    } catch (error) {
      let errorMessage = 'Login failed. Please try again.';

      if (error instanceof HttpErrorResponse) {
        if (error.status === 401) {
          errorMessage = 'Invalid email or password.';
        } else if (error.status === 400) {
          // Handle validation errors
          const problemDetails = error.error as any;
          if (problemDetails?.title) {
            errorMessage = problemDetails.title;
          } else if (problemDetails?.errors) {
            const firstError = Object.values(problemDetails.errors)[0] as string[];
            errorMessage = firstError[0];
          }
        }
      }

      return { success: false, error: errorMessage };
    }
  }

  logout(): void {
    this.accessTokenSignal.set(null);
    this.userSignal.set(null);
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
    this.router.navigate(['/login']);
  }

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) return false;

      const response = await firstValueFrom(
        this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/refresh-token`, {
          refreshToken
        })
      );

      this.accessTokenSignal.set(response.accessToken);
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);

      return true;
    } catch {
      this.logout();
      return false;
    }
  }
}
