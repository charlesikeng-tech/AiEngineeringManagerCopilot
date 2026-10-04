import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { PublicSsoApi } from '@core/auth/public-sso-api';
import { AccountLinkApi } from './account-link-api';

describe('AccountLinkApi', () => {
  let api: AccountLinkApi;
  let http: HttpTestingController;
  const root = `${environment.apiUrl}/auth/account/identities`;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AccountLinkApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('loads server-authoritative local proof eligibility and safe identities', () => {
    api.list().subscribe();
    http.expectOne(root).flush({ canLink: false, identities: [] });
    api.providers().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/sso/login/providers`).flush([]);
  });
  it('sends explicit password and consent without a client-selected target or role', () => {
    api.start('provider', 'ephemeral-password', true).subscribe();
    const request = http.expectOne(root + '/start');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      providerId: 'provider', password: 'ephemeral-password', approveAdministratorAccess: true,
    });
    request.flush({ authorizationUrl: 'https://example.auth0.com/authorize' });
  });
  it('requires a password on unlink and validates navigation through the shared HTTPS helper', () => {
    api.unlink('identity', 'password').subscribe();
    const request = http.expectOne(root + '/identity/unlink');
    expect(request.request.body).toEqual({ password: 'password' });
    request.flush(null);
    expect(() => api.navigate('javascript:alert(1)')).toThrow();
    expect(() => api.navigate('https://user:password@example.com/')).toThrow();
    const navigate = vi.spyOn(TestBed.inject(PublicSsoApi), 'navigate').mockImplementation(() => {});
    api.navigate('https://example.auth0.com/authorize');
    expect(navigate).toHaveBeenCalledWith('https://example.auth0.com/authorize');
  });
});
