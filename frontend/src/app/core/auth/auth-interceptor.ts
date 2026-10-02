import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { Auth } from './auth';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(Auth);
  const token = auth.getToken();

  const api = new URL(environment.apiUrl, document.baseURI);
  const target = new URL(request.url, document.baseURI);
  const apiPath = api.pathname.replace(/\/$/, '');
  const isApiRequest = target.origin === api.origin &&
    (target.pathname === apiPath || target.pathname.startsWith(`${apiPath}/`));

  if (!token || !isApiRequest) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });

  return next(authenticatedRequest);
};
