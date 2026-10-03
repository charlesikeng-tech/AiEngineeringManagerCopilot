import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from '@core/auth/auth-interceptor';
import { environment } from '@environments/environment';
import { Auth } from './auth';

describe('authInterceptor', () => {
  const clearSession = vi.fn();
  const interceptor: HttpInterceptorFn = (req, next) =>
    TestBed.runInInjectionContext(() => authInterceptor(req, next));

  beforeEach(() => {
    clearSession.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Auth, useValue: { getToken: () => null, clearSession } },
      ],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('sends cookie credentials and session protection to the API without a demo bearer', () => {
    TestBed.inject(HttpClient).post(`${environment.apiUrl}/auth/login`, {}).subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne(`${environment.apiUrl}/auth/login`);
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-Session-Protection')).toBe('1');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('does not send session credentials to unrelated or lookalike URLs', () => {
    for (const url of ['https://external.example/data', `${environment.apiUrl}-untrusted/data`]) {
      TestBed.inject(HttpClient).get(url).subscribe();
      const request = TestBed.inject(HttpTestingController).expectOne(url);
      expect(request.request.withCredentials).toBe(false);
      expect(request.request.headers.has('X-Session-Protection')).toBe(false);
      request.flush({});
    }
  });

  it('clears cached authentication on an API 401 and propagates the error', () => {
    const onError = vi.fn();
    TestBed.inject(HttpClient).get(`${environment.apiUrl}/teams`).subscribe({ error: onError });
    TestBed.inject(HttpTestingController).expectOne(`${environment.apiUrl}/teams`)
      .flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(clearSession).toHaveBeenCalledOnce();
    expect(onError).toHaveBeenCalledOnce();
  });

  it('does not clear authentication for a forbidden response or an external 401', () => {
    TestBed.inject(HttpClient).get(`${environment.apiUrl}/teams`).subscribe({ error: () => {} });
    TestBed.inject(HttpTestingController).expectOne(`${environment.apiUrl}/teams`)
      .flush({}, { status: 403, statusText: 'Forbidden' });
    TestBed.inject(HttpClient).get('https://external.example/data').subscribe({ error: () => {} });
    TestBed.inject(HttpTestingController).expectOne('https://external.example/data')
      .flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(clearSession).not.toHaveBeenCalled();
  });
});
