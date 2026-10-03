import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { Auth } from './auth';

export const authGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);
  if (auth.user()) return true;
  return auth.setupStatus().pipe(
    map(({ setupAvailable }) => router.parseUrl(setupAvailable ? '/setup' : '/admin/login')),
    catchError(() => of(router.parseUrl('/admin/login'))),
  );
};
