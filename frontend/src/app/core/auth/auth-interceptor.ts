import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { environment } from '@environments/environment';
import { Auth } from './auth';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(Auth);
  const token = auth.getToken();

  const isApiRequest = request.url === environment.apiUrl ||
    request.url.startsWith(`${environment.apiUrl}/`);

  if (!isApiRequest) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    withCredentials: true,
    setHeaders: {
      'X-Session-Protection': '1',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        auth.clearSession();
      }
      return throwError(() => error);
    }),
  );
};
