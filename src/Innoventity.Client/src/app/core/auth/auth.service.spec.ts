import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { ActorType } from '../models/user.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let mockRouter: { navigate: jest.Mock };

  function setup(opts: { token?: string; refreshToken?: string; user?: any } = {}) {
    localStorage.clear();
    if (opts.token) localStorage.setItem('accessToken', opts.token);
    if (opts.refreshToken) localStorage.setItem('refreshToken', opts.refreshToken);
    if (opts.user) localStorage.setItem('currentUser', JSON.stringify(opts.user));

    mockRouter = { navigate: jest.fn() };
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: mockRouter }
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    httpMock?.verify();
    localStorage.clear();
  });

  describe('initialization', () => {
    it('should be created', () => {
      setup();
      expect(service).toBeTruthy();
    });

    it('should not be authenticated when no token in localStorage', () => {
      setup();
      expect(service.isAuthenticated()).toBe(false);
      expect(service.accessToken()).toBeNull();
      expect(service.currentUser()).toBeNull();
    });

    it('should be authenticated when token exists in localStorage', () => {
      setup({ token: 'stored-token' });
      expect(service.isAuthenticated()).toBe(true);
      expect(service.accessToken()).toBe('stored-token');
    });

    it('should restore user from localStorage when token and user exist', () => {
      const mockUser = { actorId: '123', fullName: 'Test User', actorType: ActorType.IdeaGenerator };
      setup({ token: 'stored-token', user: mockUser });
      expect(service.currentUser()).toEqual(mockUser);
    });

    it('should not restore user when no token even if user is stored', () => {
      const mockUser = { actorId: '123', fullName: 'Test User', actorType: ActorType.IdeaGenerator };
      setup({ user: mockUser });
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('login', () => {
    beforeEach(() => setup());

    const mockActor = { actorId: '123', fullName: 'Test User', actorType: ActorType.IdeaGenerator };

    it('should store tokens, update signals, and return success on 200', async () => {
      const mockResponse = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        actor: mockActor
      };

      const loginPromise = service.login('test@example.com', 'password123', ActorType.IdeaGenerator);
      const req = httpMock.expectOne('/auth/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'test@example.com', password: 'password123', actorType: ActorType.IdeaGenerator });
      req.flush(mockResponse);

      const result = await loginPromise;
      expect(result.success).toBe(true);
      expect(service.isAuthenticated()).toBe(true);
      expect(service.accessToken()).toBe('new-access-token');
      expect(service.currentUser()).toEqual(mockActor);
      expect(localStorage.getItem('accessToken')).toBe('new-access-token');
      expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
    });

    it('should return "Invalid email or password" on 401', async () => {
      const loginPromise = service.login('test@example.com', 'wrongpass', ActorType.IdeaGenerator);
      httpMock.expectOne('/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });

      const result = await loginPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Invalid email or password.');
    });

    it('should return problemDetails.title on 400 with title field', async () => {
      const loginPromise = service.login('test@example.com', 'password123', ActorType.IdeaGenerator);
      httpMock.expectOne('/auth/login').flush(
        { title: 'Account is locked out' },
        { status: 400, statusText: 'Bad Request' }
      );

      const result = await loginPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Account is locked out');
    });

    it('should return first field validation error on 400 with errors field', async () => {
      const loginPromise = service.login('bad-email', 'password123', ActorType.IdeaGenerator);
      httpMock.expectOne('/auth/login').flush(
        { errors: { Email: ['Email is not a valid email address.'] } },
        { status: 400, statusText: 'Bad Request' }
      );

      const result = await loginPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Email is not a valid email address.');
    });

    it('should return generic error message for 500', async () => {
      const loginPromise = service.login('test@example.com', 'password123', ActorType.IdeaGenerator);
      httpMock.expectOne('/auth/login').flush({}, { status: 500, statusText: 'Server Error' });

      const result = await loginPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Login failed. Please try again.');
    });
  });

  describe('logout', () => {
    it('should clear signals, clear localStorage, and navigate to /login', () => {
      setup({ token: 'some-token' });
      localStorage.setItem('refreshToken', 'refresh-token');
      localStorage.setItem('currentUser', JSON.stringify({ actorId: '1' }));

      service.logout();

      expect(service.isAuthenticated()).toBe(false);
      expect(service.accessToken()).toBeNull();
      expect(service.currentUser()).toBeNull();
      expect(localStorage.getItem('accessToken')).toBeNull();
      expect(localStorage.getItem('refreshToken')).toBeNull();
      expect(localStorage.getItem('currentUser')).toBeNull();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('refreshToken', () => {
    it('should return false immediately when no refresh token in localStorage', async () => {
      setup();
      const result = await service.refreshToken();
      expect(result).toBe(false);
    });

    it('should update tokens and return true on successful refresh', async () => {
      setup({ refreshToken: 'old-refresh-token' });
      const mockResponse = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        actor: { actorId: '123', fullName: 'Test', actorType: ActorType.IdeaGenerator }
      };

      const refreshPromise = service.refreshToken();
      httpMock.expectOne('/auth/refresh-token').flush(mockResponse);

      const result = await refreshPromise;
      expect(result).toBe(true);
      expect(service.accessToken()).toBe('new-access-token');
      expect(localStorage.getItem('accessToken')).toBe('new-access-token');
      expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
    });

    it('should call logout and return false on refresh failure', async () => {
      setup({ refreshToken: 'expired-token' });
      jest.spyOn(service, 'logout').mockImplementation(() => {});

      const refreshPromise = service.refreshToken();
      httpMock.expectOne('/auth/refresh-token').flush(
        {},
        { status: 401, statusText: 'Unauthorized' }
      );

      const result = await refreshPromise;
      expect(result).toBe(false);
      expect(service.logout).toHaveBeenCalled();
    });
  });
});
