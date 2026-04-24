import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.hasValidSession()) {
    authService.invalidateSession();
    return router.createUrlTree(['/ingreso']);
  }

  return authService.fetchProfile().pipe(
    map(() => true),
    catchError(() => {
      authService.invalidateSession();
      return of(router.createUrlTree(['/ingreso']));
    })
  );
};
