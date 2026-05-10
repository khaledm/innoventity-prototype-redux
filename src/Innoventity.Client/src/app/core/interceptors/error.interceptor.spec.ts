import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../auth/auth.service';

describe('errorInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let mockRouter: { navigate: jest.Mock };
  let mockAuthService: { logout: jest.Mock };

  beforeEach(() => {
    mockRouter = { navigate: jest.fn() };
    mockAuthService = { logout: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: mockRouter },
        { provide: AuthService, useValue: mockAuthService }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call authService.logout and navigate with sessionExpired on 401', () => {
    httpClient.get('/some-resource').subscribe({ error: () => {} });
    httpMock.expectOne('/some-resource').flush(
      {},
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(mockAuthService.logout).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(
      ['/login'],
      { queryParams: { sessionExpired: 'true' } }
    );
  });

  it('should navigate to /forbidden on 403', () => {
    httpClient.get('/some-resource').subscribe({ error: () => {} });
    httpMock.expectOne('/some-resource').flush(
      {},
      { status: 403, statusText: 'Forbidden' }
    );

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/forbidden']);
    expect(mockAuthService.logout).not.toHaveBeenCalled();
  });

  it('should log console.error on 500', () => {
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    httpClient.get('/some-resource').subscribe({ error: () => {} });
    httpMock.expectOne('/some-resource').flush(
      {},
      { status: 500, statusText: 'Internal Server Error' }
    );

    expect(consoleSpy).toHaveBeenCalledWith('Server error:', expect.anything());
    consoleSpy.mockRestore();
  });

  it('should re-throw the error for component-level handling on 404', (done) => {
    httpClient.get('/some-resource').subscribe({
      error: (err) => {
        expect(err.status).toBe(404);
        expect(mockRouter.navigate).not.toHaveBeenCalled();
        done();
      }
    });
    httpMock.expectOne('/some-resource').flush(
      {},
      { status: 404, statusText: 'Not Found' }
    );
  });

  it('should pass successful responses through without modification', () => {
    let response: any;
    httpClient.get('/some-resource').subscribe({ next: (r) => { response = r; } });
    httpMock.expectOne('/some-resource').flush({ data: 'success' });

    expect(response).toEqual({ data: 'success' });
    expect(mockRouter.navigate).not.toHaveBeenCalled();
    expect(mockAuthService.logout).not.toHaveBeenCalled();
  });
});
