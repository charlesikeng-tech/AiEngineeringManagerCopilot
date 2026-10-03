import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from './auth';

export const administratorGuard: CanActivateFn = () =>
  inject(Auth).user()?.role === 'PlatformAdministrator' || inject(Router).parseUrl('/admin/login');
