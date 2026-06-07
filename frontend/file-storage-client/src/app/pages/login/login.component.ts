import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, UserRole } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  isSubmitting = false;
  errorMessage = '';

  readonly form = this.formBuilder.nonNullable.group({
    userId: ['user-1', Validators.required],
    role: ['user' as UserRole, Validators.required]
  });

  constructor(
    private readonly authService: AuthService,
    private readonly formBuilder: FormBuilder,
    private readonly router: Router
  ) {}

  login(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.router.navigateByUrl('/files');
      },
      error: () => {
        this.errorMessage = 'Could not create a token. Make sure the backend API is running.';
        this.isSubmitting = false;
      }
    });
  }
}
