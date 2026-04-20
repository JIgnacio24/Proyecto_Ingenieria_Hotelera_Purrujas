import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AdminUser, AuthResponse, LoginPayload, RegisterPayload } from './auth.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storageKey = 'purrujas.admin.session';
  private readonly apiBaseUrl = environment.apiBaseUrl;
  private readonly sessionState = signal<AuthResponse | null>(this.readStoredSession());

  readonly session = this.sessionState.asReadonly();
  readonly currentUser = computed(() => this.sessionState()?.user ?? null);
  readonly isAuthenticated = computed(() => this.hasValidSession());

  login(payload: LoginPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/login`, payload)
      .pipe(tap((session) => this.persistSession(session)));
  }

  register(payload: RegisterPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/register`, payload)
      .pipe(tap((session) => this.persistSession(session)));
  }

  fetchProfile(): Observable<AdminUser> {
    return this.http.get<AdminUser>(`${this.apiBaseUrl}/auth/me`).pipe(
      tap((user) => {
        const session = this.sessionState();
        if (!session) {
          return;
        }

        this.persistSession({
          ...session,
          user
        });
      })
    );
  }

  getToken(): string | null {
    const session = this.sessionState();
    if (!session) {
      return null;
    }

    if (this.isExpired(session.expiresAt)) {
      this.clearSession();
      return null;
    }

    return session.token;
  }

  hasValidSession(): boolean {
    const session = this.sessionState();
    if (!session) {
      return false;
    }

    if (this.isExpired(session.expiresAt)) {
      this.clearSession();
      return false;
    }

    return true;
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/ingreso']);
  }

  private persistSession(session: AuthResponse): void {
    if (this.isExpired(session.expiresAt)) {
      this.clearSession();
      return;
    }

    this.sessionState.set(session);

    if (this.canUseStorage()) {
      localStorage.setItem(this.storageKey, JSON.stringify(session));
    }
  }

  private readStoredSession(): AuthResponse | null {
    if (!this.canUseStorage()) {
      return null;
    }

    const rawSession = localStorage.getItem(this.storageKey);
    if (!rawSession) {
      return null;
    }

    try {
      const session = JSON.parse(rawSession) as AuthResponse;
      if (!session?.token || !session?.expiresAt || !session?.user) {
        this.clearSession();
        return null;
      }

      if (this.isExpired(session.expiresAt)) {
        this.clearSession();
        return null;
      }

      return session;
    } catch {
      this.clearSession();
      return null;
    }
  }

  private clearSession(): void {
    this.sessionState.set(null);

    if (this.canUseStorage()) {
      localStorage.removeItem(this.storageKey);
    }
  }

  private isExpired(expiresAt: string): boolean {
    const expiryTime = new Date(expiresAt).getTime();
    return Number.isNaN(expiryTime) || expiryTime <= Date.now();
  }

  private canUseStorage(): boolean {
    return typeof window !== 'undefined' && typeof localStorage !== 'undefined';
  }
}
