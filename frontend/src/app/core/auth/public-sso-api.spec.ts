import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { PublicSsoApi } from './public-sso-api';

describe('Public SSO API', () => {
  let api: PublicSsoApi;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(PublicSsoApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('loads only the public provider list and starts via POST without credentials', () => {
    api.providers().subscribe((providers) => expect(providers[0].name).toBe('Company Okta'));
    http.expectOne(`${environment.apiUrl}/auth/sso/login/providers`).flush([
      { id: 'provider', name: 'Company Okta', type: 'Okta' },
    ]);
    api.start('provider').subscribe();
    const start = http.expectOne(`${environment.apiUrl}/auth/sso/login/provider/start`);
    expect(start.request.method).toBe('POST');
    expect(start.request.body).toEqual({});
    start.flush({ authorizationUrl: 'https://company.okta.com/authorize' });
  });

  it('refuses unsafe navigation destinations', () => {
    expect(() => api.navigate('javascript:alert(1)')).toThrow();
    expect(() => api.navigate('/dashboard')).toThrow();
    expect(() => api.navigate('https://username:password@company.okta.com/authorize')).toThrow();
  });
});
