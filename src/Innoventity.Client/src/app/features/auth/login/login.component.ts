import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../../core/auth/auth.service';
import { ActorType } from '../../../core/models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  loginForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    actorType: new FormControl<ActorType | ''>('', Validators.required),
    password: new FormControl('', [Validators.required, Validators.minLength(8)])
  });

  // Actor type options for dropdown
  actorTypes = [
    { value: ActorType.IdeaGenerator, label: 'Idea Generator' },
    { value: ActorType.Investor, label: 'Investor' },
    { value: ActorType.RDOrganization, label: 'R&D Organization' },
    { value: ActorType.Manufacturing, label: 'Manufacturing' },
    { value: ActorType.SalesMarketing, label: 'Sales & Marketing' }
  ];

  // Note: Use loginForm.valid directly in template instead of computed signal
  // Computed signals don't automatically track FormGroup validity changes

  emailError = computed(() => {
    const control = this.loginForm.get('email');
    if (control?.touched) {
      if (control?.hasError('required')) return 'Email is required';
      if (control?.hasError('email')) return 'Invalid email format';
    }
    return null;
  });

  passwordError = computed(() => {
    const control = this.loginForm.get('password');
    if (control?.touched) {
      if (control?.hasError('required')) return 'Password is required';
      if (control?.hasError('minlength')) return 'Password must be at least 8 characters';
    }
    return null;
  });

  actorTypeError = computed(() => {
    const control = this.loginForm.get('actorType');
    if (control?.touched && control?.hasError('required')) {
      return 'Please select your actor type';
    }
    return null;
  });

  // Async state signals
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, actorType, password } = this.loginForm.value;
    if (!email || !actorType || !password) return;

    const result = await this.authService.login(email, password, actorType as ActorType);

    this.isLoading.set(false);

    if (result.success) {
      // TODO: Get actual innovation ID from backend or user selection
      // For T073 testing, using a placeholder ID
      const testInnovationId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
      this.router.navigate(['/innovations', testInnovationId]);
    } else {
      this.errorMessage.set(result.error || 'Login failed. Please try again.');
    }
  }
}
