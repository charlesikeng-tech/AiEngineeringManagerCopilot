import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { Auth } from './auth';
import { authGuard } from './auth-guard';

describe('Protected navigation', () => {
  const auth = { user: () => null as unknown, setupStatus: () => of({ setupAvailable: false }) };
  const guard = () => TestBed.runInInjectionContext(() =>
    authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
  beforeEach(() => {
    auth.user = () => null;
    auth.setupStatus = () => of({ setupAvailable: false });
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Auth, useValue: auth }],
    });
  });

  it('permits an authenticated administrator', () => {
    auth.user = () => ({ id: 'admin' });
    expect(guard()).toBe(true);
  });

  it('redirects to setup only when the server explicitly makes it available', async () => {
    auth.setupStatus = () => of({ setupAvailable: true });
    expect((await firstValueFrom(guard() as Observable<UrlTree>)).toString()).toBe('/setup');
    auth.setupStatus = () => of({ setupAvailable: false });
    expect((await firstValueFrom(guard() as Observable<UrlTree>)).toString()).toBe('/admin/login');
  });

  it('never assumes setup eligibility on API failure', async () => {
    auth.setupStatus = () => throwError(() => new Error('Unavailable'));
    expect((await firstValueFrom(guard() as Observable<UrlTree>)).toString()).toBe('/admin/login');
  });
});
