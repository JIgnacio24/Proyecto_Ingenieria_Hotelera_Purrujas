import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, Observable, of, tap } from 'rxjs';
import {
  ADMINISTRATOR_ROLE,
  AdminUser,
  AuthResponse,
  LoginPayload,
  RegisterPayload
} from './auth.models';
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

  register(payload: RegisterPayload, persistSession = true): Observable<AuthResponse> {
    const request = this.http.post<AuthResponse>(`${this.apiBaseUrl}/auth/register`, payload);
    return persistSession ? request.pipe(tap((session) => this.persistSession(session))) : request;
  }

  fetchProfile(): Observable<AdminUser> {
    return this.http.get<AdminUser>(`${this.apiBaseUrl}/auth/me`).pipe(
      tap((user) => {
        if (!this.isAdministratorUser(user)) {
          this.invalidateSession();
          throw new Error('La sesion no pertenece a un administrador autorizado.');
        }

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

    if (!this.isAdministrativeSession(session)) {
      this.invalidateSession();
      return null;
    }

    return session.token;
  }

  hasValidSession(): boolean {
    const session = this.sessionState();
    if (!session) {
      return false;
    }

    if (!this.isAdministrativeSession(session)) {
      this.invalidateSession();
      return false;
    }

    return true;
  }

  invalidateSession(): void {
    this.clearSession();
  }

  logout(): void {
    const logoutRequest = this.hasValidSession()
      ? this.http.post<void>(`${this.apiBaseUrl}/auth/logout`, {}).pipe(catchError(() => of(void 0)))
      : of(void 0);

    void firstValueFrom(logoutRequest).finally(() => {
      void this.finalizeLogout();
    });
  }

  private async finalizeLogout(): Promise<void> {
    await this.clearBrowserState();
    await this.router.navigate(['/ingreso'], { replaceUrl: true });
  }

  private persistSession(session: AuthResponse): void {
    if (!this.isAdministrativeSession(session)) {
      this.invalidateSession();
      return;
    }

    this.sessionState.set(session);
    this.sessionStorageRef()?.setItem(this.storageKey, JSON.stringify(session));
    this.legacyStorageRef()?.removeItem(this.storageKey);
  }

  private readStoredSession(): AuthResponse | null {
    const rawSession =
      this.sessionStorageRef()?.getItem(this.storageKey) ??
      this.legacyStorageRef()?.getItem(this.storageKey);

    if (!rawSession) {
      return null;
    }

    try {
      const session = JSON.parse(rawSession) as AuthResponse;
      if (!session?.token || !session?.expiresAt || !session?.user) {
        this.clearStoredSession();
        return null;
      }

      if (!this.isAdministrativeSession(session)) {
        this.clearStoredSession();
        return null;
      }

      this.sessionStorageRef()?.setItem(this.storageKey, JSON.stringify(session));
      this.legacyStorageRef()?.removeItem(this.storageKey);
      return session;
    } catch {
      this.clearStoredSession();
      return null;
    }
  }

  private clearSession(): void {
    this.sessionState.set(null);
    this.clearStoredSession();
  }

  private clearStoredSession(): void {
    this.sessionStorageRef()?.removeItem(this.storageKey);
    this.legacyStorageRef()?.removeItem(this.storageKey);
  }

  private async clearBrowserState(): Promise<void> {
    this.clearSession();

    const cacheStorage = this.cacheStorageRef();
    if (!cacheStorage) {
      return;
    }

    try {
      const cacheKeys = await cacheStorage.keys();
      await Promise.all(cacheKeys.map((key) => cacheStorage.delete(key)));
    } catch {
      // Best-effort cleanup for browsers that do not allow cache deletion here.
    }
  }

  private isExpired(expiresAt: string): boolean {
    const expiryTime = new Date(expiresAt).getTime();
    return Number.isNaN(expiryTime) || expiryTime <= Date.now();
  }

  private isAdministrativeSession(session: AuthResponse): boolean {
    return !this.isExpired(session.expiresAt) && this.isAdministratorUser(session.user);
  }

  private isAdministratorUser(user: AdminUser | null | undefined): boolean {
    return user?.role?.trim().toLowerCase() === ADMINISTRATOR_ROLE.toLowerCase();
  }

  private sessionStorageRef(): Storage | null {
    return typeof window !== 'undefined' && typeof sessionStorage !== 'undefined'
      ? sessionStorage
      : null;
  }

  private legacyStorageRef(): Storage | null {
    return typeof window !== 'undefined' && typeof localStorage !== 'undefined'
      ? localStorage
      : null;
  }

  private cacheStorageRef(): CacheStorage | null {
    return typeof window !== 'undefined' && 'caches' in window ? window.caches : null;
  }
}
