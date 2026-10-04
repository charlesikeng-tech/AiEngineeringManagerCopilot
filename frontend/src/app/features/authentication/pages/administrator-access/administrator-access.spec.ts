import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { AppInitializer } from '@core/app/app-initializer';
import { of, throwError } from 'rxjs';
import { AdministratorAccess } from './administrator-access';
import { Auth } from '@core/auth/auth';

describe('Administrator access page', () => {
  const auth = {
    user: () => null,
    setupStatus: vi.fn(() => of({ setupAvailable: true })),
    setup: vi.fn(() => of({})),
    login: vi.fn(() => of({})),
  };
  const initializer = { initializeTeams: vi.fn(() => of(undefined)) };
  let route: { snapshot: { data: { setup: boolean } } };

  beforeEach(() => {
    vi.clearAllMocks();
    route = { snapshot: { data: { setup: false } } };
    TestBed.configureTestingModule({
      imports: [AdministratorAccess],
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: auth },
        { provide: AppInitializer, useValue: initializer },
        { provide: ActivatedRoute, useValue: route },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  it('resumes team initialization after successful login and clears the password', () => {
    const component = TestBed.createComponent(AdministratorAccess).componentInstance;
    component.email = 'admin@test.example';
    component.password = 'password';
    component.submit();
    expect(auth.login).toHaveBeenCalledWith(component.email, 'password');
    expect(initializer.initializeTeams).toHaveBeenCalled();
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/dashboard');
    expect(component.password).toBe('');
  });
  it('keeps local recovery accessible even when an SSO administrator is signed in', () => {
    const user = vi.spyOn(auth, 'user').mockReturnValue({ role: 'PlatformAdministrator' } as never);
    TestBed.createComponent(AdministratorAccess).componentInstance.ngOnInit();
    expect(TestBed.inject(Router).navigateByUrl).not.toHaveBeenCalled();
    user.mockRestore();
  });

  it('does not submit installation when password confirmation differs', () => {
    route.snapshot.data.setup = true;
    const component = TestBed.createComponent(AdministratorAccess).componentInstance;
    component.password = 'first';
    component.confirmation = 'second';
    component.submit();
    expect(auth.setup).not.toHaveBeenCalled();
    expect(component.error()).toContain('correspondent');
  });

  it('redirects unavailable first-run setup to administrator login', () => {
    route.snapshot.data.setup = true;
    auth.setupStatus.mockReturnValueOnce(of({ setupAvailable: false }));
    const component = TestBed.createComponent(AdministratorAccess).componentInstance;
    component.ngOnInit();
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/admin/login');
  });

  it('shows a generic error for invalid credentials without navigating', () => {
    auth.login.mockReturnValueOnce(throwError(() => ({ status: 401 })));
    const component = TestBed.createComponent(AdministratorAccess).componentInstance;
    component.submit();
    expect(component.error()).toContain('Connexion refusée');
    expect(component.busy()).toBe(false);
    expect(TestBed.inject(Router).navigateByUrl).not.toHaveBeenCalled();
  });

  it('surfaces team loading failure after login without navigating', () => {
    initializer.initializeTeams.mockReturnValueOnce(throwError(() => ({ status: 503 })));
    const component = TestBed.createComponent(AdministratorAccess).componentInstance;
    component.submit();
    expect(component.error()).toContain('chargement des équipes');
    expect(component.busy()).toBe(false);
    expect(TestBed.inject(Router).navigateByUrl).not.toHaveBeenCalled();
  });
});
