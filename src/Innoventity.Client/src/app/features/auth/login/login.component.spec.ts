import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Location } from '@angular/common';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';
import { ActorType } from '../../../core/models/user.model';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthService: jest.Mocked<AuthService>;
  let router: Router;
  let location: Location;

  beforeEach(async () => {
    mockAuthService = {
      login: jest.fn(),
      logout: jest.fn(),
      refreshToken: jest.fn(),
      isAuthenticated: jest.fn().mockReturnValue(false),
      currentUser: jest.fn().mockReturnValue(null),
      accessToken: jest.fn().mockReturnValue(null)
    } as any;

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        HttpClientTestingModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        provideRouter([
          { path: 'login', component: LoginComponent },
          { path: 'register', component: LoginComponent }, // Dummy route for testing
          { path: 'innovations', component: LoginComponent }
        ])
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    jest.spyOn(router, 'navigate');

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty form', () => {
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
    expect(component.loginForm.get('actorType')?.value).toBe('');
  });

  it('should validate required fields', () => {
    const form = component.loginForm;
    expect(form.valid).toBeFalsy();

    form.patchValue({
      email: 'test@example.com',
      password: 'password123',
      actorType: ActorType.IdeaGenerator
    });

    expect(form.valid).toBeTruthy();
  });

  it('should validate email format', () => {
    const emailControl = component.loginForm.get('email');

    emailControl?.setValue('invalid-email');
    expect(emailControl?.hasError('email')).toBeTruthy();

    emailControl?.setValue('valid@example.com');
    expect(emailControl?.hasError('email')).toBeFalsy();
  });

  it('should validate password minimum length', () => {
    const passwordControl = component.loginForm.get('password');

    passwordControl?.setValue('short');
    expect(passwordControl?.hasError('minlength')).toBeTruthy();

    passwordControl?.setValue('longenoughpassword');
    expect(passwordControl?.hasError('minlength')).toBeFalsy();
  });

  it('should disable submit button when form is invalid', () => {
    expect(component.isFormValid()).toBeFalsy();
  });

  it('should enable submit button when form is valid', () => {
    const emailCtrl = component.loginForm.get('email');
    const passwordCtrl = component.loginForm.get('password');
    const actorTypeCtrl = component.loginForm.get('actorType');

    emailCtrl?.setValue('test@example.com');
    passwordCtrl?.setValue('password123');
    actorTypeCtrl?.setValue(ActorType.IdeaGenerator);

    expect(emailCtrl?.valid).toBeTruthy();
    expect(passwordCtrl?.valid).toBeTruthy();
    expect(actorTypeCtrl?.valid).toBeTruthy();
    expect(component.loginForm.valid).toBeTruthy();
  });

  it('should call authService.login on form submit with valid credentials', async () => {
    mockAuthService.login.mockResolvedValue({ success: true });

    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123',
      actorType: ActorType.IdeaGenerator
    });

    await component.onSubmit();

    expect(mockAuthService.login).toHaveBeenCalledWith(
      'test@example.com',
      'password123',
      ActorType.IdeaGenerator
    );
  });

  it('should navigate to innovations on successful login', async () => {
    mockAuthService.login.mockResolvedValue({ success: true });

    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123',
      actorType: ActorType.IdeaGenerator
    });

    await component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/innovations', '3fa85f64-5717-4562-b3fc-2c963f66afa6']);
  });

  it('should display error message on failed login', async () => {
    const errorMsg = 'Invalid credentials';
    mockAuthService.login.mockResolvedValue({
      success: false,
      error: errorMsg
    });

    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'wrongpassword',
      actorType: ActorType.IdeaGenerator
    });

    await component.onSubmit();

    expect(component.errorMessage()).toBe(errorMsg);
  });

  it('should show loading state during login', async () => {
    let resolveLogin: (value: any) => void;
    const loginPromise = new Promise(resolve => {
      resolveLogin = resolve;
    });
    mockAuthService.login.mockReturnValue(loginPromise as any);

    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123',
      actorType: ActorType.IdeaGenerator
    });

    const submitPromise = component.onSubmit();
    expect(component.isLoading()).toBeTruthy();

    resolveLogin!({ success: true });
    await submitPromise;
    expect(component.isLoading()).toBeFalsy();
  });

  it('should not submit when form is invalid', async () => {
    component.loginForm.patchValue({
      email: '',
      password: '',
      actorType: ''
    });

    await component.onSubmit();

    expect(mockAuthService.login).not.toHaveBeenCalled();
  });
});

