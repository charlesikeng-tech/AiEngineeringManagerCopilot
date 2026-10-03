import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { SsoApi, SsoProvider } from './sso-api';

describe('SsoApi', () => {
  let api: SsoApi;
  let http: HttpTestingController;
  const root = `${environment.apiUrl}/auth/sso/providers`;
  const provider = { id: 'provider-id', revision: 3 } as SsoProvider;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(SsoApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('loads redacted configuration and fixed callback instructions', () => {
    api.list().subscribe();
    http.expectOne(root).flush({ callbackUrl: 'https://api.example/auth/sso/callback', providers: [] });
  });
  it('uses an explicit revision and secret action when saving', () => {
    api.save('provider-id', {
      revision: 3, type: 'Auth0', name: 'Test', tenant: 'example.auth0.com',
      authorizationServer: 'default', clientId: 'client', secretAction: 'retain',
    }).subscribe();
    const request = http.expectOne(`${root}/provider-id`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.secretAction).toBe('retain');
    expect(request.request.body.clientSecret).toBeUndefined();
    request.flush({});
  });
  it('pins test and activation to the saved revision', () => {
    api.test(provider).subscribe();
    const test = http.expectOne(`${root}/provider-id/test`);
    expect(test.request.body).toEqual({ revision: 3 });
    test.flush({ authorizationUrl: 'https://example.auth0.com/authorize' });
    api.activate(provider).subscribe();
    const activation = http.expectOne(`${root}/provider-id/activate`);
    expect(activation.request.body).toEqual({ revision: 3 });
    activation.flush({});
    api.deactivate(provider).subscribe();
    http.expectOne(`${root}/provider-id/deactivate`).flush({});
    api.delete(provider).subscribe();
    const deletion = http.expectOne(`${root}/provider-id`);
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);
  });
});
