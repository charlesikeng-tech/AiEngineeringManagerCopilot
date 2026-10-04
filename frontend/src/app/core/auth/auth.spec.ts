import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { Auth, AdministratorProfile } from './auth';

describe('Local and SSO session authentication', () => {
  const profile: AdministratorProfile = {
    id: 'admin-id', name: 'Administrator', email: 'admin@test.example', role: 'PlatformAdministrator',
  };
  let auth: Auth;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    auth = TestBed.inject(Auth);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('restores a server session without obtaining a development JWT', () => {
    auth.loadCurrent().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/current`).flush(profile);
    expect(auth.user()).toEqual(profile);
    expect(auth.getToken()).toBeNull();
  });

  it('restores an ordinary SSO profile without inventing an email or acquiring tokens', () => {
    const ordinary: AdministratorProfile = {
      id: 'ordinary-id', name: 'SSO user', email: null, emailVerified: false, role: 'User',
    };
    auth.loadCurrent().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/current`).flush(ordinary);
    expect(auth.user()).toEqual(ordinary);
    expect(auth.getToken()).toBeNull();
  });

  it('treats unauthenticated startup as a normal signed-out state', () => {
    let restored: AdministratorProfile | null | undefined;
    auth.loadCurrent().subscribe((value) => restored = value);
    http.expectOne(`${environment.apiUrl}/auth/current`).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(restored).toBeNull();
    expect(auth.user()).toBeNull();
  });

  it('stores the profile after login and clears it after server logout', () => {
    auth.login(profile.email!, 'password').subscribe();
    const login = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(login.request.body).toEqual({ email: profile.email, password: 'password' });
    login.flush(profile);
    auth.logout().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/logout`).flush(null);
    expect(auth.user()).toBeNull();
  });

  it('clears an already-expired session during logout', () => {
    auth.loadCurrent().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/current`).flush(profile);
    auth.logout().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/logout`).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.user()).toBeNull();
  });
});
