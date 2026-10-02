import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { authInterceptor } from './auth-interceptor';
import { Auth } from './auth';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: Auth, useValue: { getToken: () => 'test-token' } }],
    });
  });

  function authorization(url: string): string | null {
    let value: string | null = null;
    TestBed.runInInjectionContext(() => authInterceptor(new HttpRequest('GET', url), req => {
      value = req.headers.get('Authorization');
      return of(new HttpResponse());
    })).subscribe();
    return value;
  }

  it('attaches the token to the configured API', () => {
    expect(authorization(`${environment.apiUrl}/teams`)).toBe('Bearer test-token');
  });

  it('does not leak tokens to another origin', () => {
    expect(authorization('https://attacker.example/teams')).toBeNull();
  });

  it('rejects origins that only share the API URL prefix', () => {
    const api = new URL(environment.apiUrl, document.baseURI);
    api.hostname += '.attacker.example';
    expect(authorization(api.href)).toBeNull();
  });
});
