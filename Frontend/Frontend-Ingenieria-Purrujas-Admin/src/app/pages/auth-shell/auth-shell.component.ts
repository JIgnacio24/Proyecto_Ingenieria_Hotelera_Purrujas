import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './auth-shell.component.html',
  styleUrl: './auth-shell.component.css'
})
export class AuthShellComponent {
  private readonly usernamePattern = /^[a-zA-Z0-9._-]{4,50}$/;
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal('');

  readonly loginForm = this.formBuilder.nonNullable.group({
    username: [
      '',
      [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(50),
        Validators.pattern(this.usernamePattern)
      ]
    ],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(255)]]
  });

  constructor() {
    if (this.authService.hasValidSession()) {
      void this.router.navigate(['/panel'], { replaceUrl: true });
    }
  }

  async submitLogin(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    try {
      const { username, password } = this.loginForm.getRawValue();
      await firstValueFrom(
        this.authService.login({
          username: username.trim(),
          password
        })
      );
      await this.router.navigate(['/panel'], { replaceUrl: true });
    } catch (error) {
      this.errorMessage.set(this.resolveError(error, 'No fue posible iniciar sesion.'));
    } finally {
      this.submitting.set(false);
    }
  }

  hasLoginError(controlName: 'username' | 'password'): boolean {
    const control = this.loginForm.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  private resolveError(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.message || error.message || fallbackMessage;
    }

    if (error instanceof Error && error.message) {
      return error.message;
    }

    return fallbackMessage;
  }
}
