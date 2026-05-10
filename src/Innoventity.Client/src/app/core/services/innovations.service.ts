import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { InnovationDetail, InnovationListResponse } from '../models/innovation.model';
import { Result } from '../models/result.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InnovationsService {
  // Private signals for state management
  private currentInnovationSignal = signal<InnovationDetail | null>(null);
  private loadingSignal = signal(false);
  private errorSignal = signal<string | null>(null);

  // Public computed signals
  currentInnovation = this.currentInnovationSignal.asReadonly();
  loading = this.loadingSignal.asReadonly();
  error = this.errorSignal.asReadonly();

  constructor(private http: HttpClient) {}

  async listInnovations(params?: {
    industryId?: string;
    researchCategory?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Result<InnovationListResponse>> {
    let httpParams = new HttpParams();
    if (params?.industryId) httpParams = httpParams.set('industryId', params.industryId);
    if (params?.researchCategory) httpParams = httpParams.set('researchCategory', params.researchCategory);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    try {
      const response = await firstValueFrom(
        this.http.get<InnovationListResponse>(`${environment.apiBaseUrl}/innovations`, { params: httpParams })
      );
      return { success: true, data: response };
    } catch (error) {
      let errorMessage = 'Failed to load innovations.';
      if (error instanceof HttpErrorResponse && error.status === 401) {
        errorMessage = 'Please log in to view innovations.';
      }
      return { success: false, error: errorMessage };
    }
  }

  async getInnovationById(id: string): Promise<Result<InnovationDetail>> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    try {
      const innovation = await firstValueFrom(
        this.http.get<InnovationDetail>(`${environment.apiBaseUrl}/innovations/${id}`)
      );

      this.currentInnovationSignal.set(innovation);
      this.loadingSignal.set(false);

      return { success: true, data: innovation };
    } catch (error) {
      this.loadingSignal.set(false);

      let errorMessage = 'Failed to load innovation.';

      if (error instanceof HttpErrorResponse) {
        if (error.status === 404) {
          errorMessage = 'Innovation not found.';
        } else if (error.status === 401) {
          errorMessage = 'Please log in to view this innovation.';
        } else if (error.status === 403) {
          errorMessage = 'You do not have permission to view this innovation.';
        }
      }

      this.errorSignal.set(errorMessage);
      return { success: false, error: errorMessage };
    }
  }

  clearError(): void {
    this.errorSignal.set(null);
  }
}
