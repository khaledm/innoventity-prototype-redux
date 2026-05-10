import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('AuthGuard', () => {
  let mockAuthService: { isAuthenticated: jest.Mock };
  let mockRouter: { navigate: jest.Mock };

  function runGuard(): boolean {
    return TestBed.runInInjectionContext(() => AuthGuard()) as boolean;
  }

  beforeEach(() => {
    mockAuthService = { isAuthenticated: jest.fn() };
    mockRouter = { navigate: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ]
    });
  });

  it('should return true and not navigate when user is authenticated', () => {
    mockAuthService.isAuthenticated.mockReturnValue(true);

    const result = runGuard();

    expect(result).toBe(true);
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should return false and navigate to /login when user is not authenticated', () => {
    mockAuthService.isAuthenticated.mockReturnValue(false);

    const result = runGuard();

    expect(result).toBe(false);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });
});
