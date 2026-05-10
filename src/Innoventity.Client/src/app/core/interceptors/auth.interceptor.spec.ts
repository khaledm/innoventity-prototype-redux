import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';

describe('authInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let mockAuthService: { accessToken: jest.Mock };

  function setup(token: string | null) {
    mockAuthService = { accessToken: jest.fn().mockReturnValue(token) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('should add Authorization header when token exists and URL is not an auth endpoint', () => {
    setup('my-token');

    httpClient.get('/innovations').subscribe();
    const req = httpMock.expectOne('/innovations');

    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush({});
  });

  it('should NOT add Authorization header for /auth/ URLs even when token exists', () => {
    setup('my-token');

    httpClient.post('/auth/login', {}).subscribe();
    const req = httpMock.expectOne('/auth/login');

    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should NOT add Authorization header when no token', () => {
    setup(null);

    httpClient.get('/innovations').subscribe();
    const req = httpMock.expectOne('/innovations');

    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should pass the request through unchanged for /auth/refresh-token', () => {
    setup('my-token');

    httpClient.post('/auth/refresh-token', { refreshToken: 'abc' }).subscribe();
    const req = httpMock.expectOne('/auth/refresh-token');

    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
