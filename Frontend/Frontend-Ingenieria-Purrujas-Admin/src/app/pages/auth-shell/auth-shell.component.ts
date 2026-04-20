import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

type AuthMode = 'login' | 'register';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './auth-shell.component.html',
  styleUrl: './auth-shell.component.css'
})
export class AuthShellComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly mode = signal<AuthMode>('login');
  readonly submitting = signal(false);
  readonly errorMessage = signal('');

  readonly loginForm = this.formBuilder.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  readonly registerForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(6)]],
    username: ['', [Validators.required, Validators.minLength(4)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  constructor() {
    if (this.authService.hasValidSession()) {
      void this.router.navigate(['/panel']);
    }
  }

  setMode(mode: AuthMode): void {
    this.mode.set(mode);
    this.errorMessage.set('');
  }

  async submitLogin(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    try {
      await firstValueFrom(this.authService.login(this.loginForm.getRawValue()));
      await this.router.navigate(['/panel']);
    } catch (error) {
      this.errorMessage.set(this.resolveError(error, 'No fue posible iniciar sesion.'));
    } finally {
      this.submitting.set(false);
    }
  }

  async submitRegister(): Promise<void> {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const { password, confirmPassword } = this.registerForm.getRawValue();
    if (password !== confirmPassword) {
      this.registerForm.controls.confirmPassword.setErrors({ mismatch: true });
      this.registerForm.controls.confirmPassword.markAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    try {
      const { fullName, username, email } = this.registerForm.getRawValue();
      await firstValueFrom(
        this.authService.register({
          fullName,
          username,
          email,
          password,
          role: 'Administrador'
        })
      );

      await this.router.navigate(['/panel']);
    } catch (error) {
      this.errorMessage.set(this.resolveError(error, 'No fue posible registrar el usuario.'));
    } finally {
      this.submitting.set(false);
    }
  }

  hasLoginError(controlName: 'username' | 'password'): boolean {
    const control = this.loginForm.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  hasRegisterError(
    controlName: 'fullName' | 'username' | 'email' | 'password' | 'confirmPassword'
  ): boolean {
    const control = this.registerForm.controls[controlName];
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
