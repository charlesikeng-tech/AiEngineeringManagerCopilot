import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { of, Subject, throwError } from 'rxjs';
import { Auth } from '@core/auth/auth';
import { PublicSsoApi } from '../../data-access/public-sso-api';
import { SsoLogin } from './sso-login';
import authenticationEn from '../../../../../../public/i18n/authentication/en.json';
import authenticationFr from '../../../../../../public/i18n/authentication/fr.json';
import accountEn from '../../../../../../public/i18n/account/en.json';
import accountFr from '../../../../../../public/i18n/account/fr.json';

describe('Public SSO login', () => {
  const api = {
    providers: vi.fn(() => of([{ id: 'okta', name: 'Company Okta', type: 'Okta' }])),
    start: vi.fn(() => of({ authorizationUrl: 'https://company.okta.com/authorize' })),
    navigate: vi.fn(),
  };
  let user: { role: 'User' } | null;
  let failure: string | null;

  beforeEach(() => {
    vi.clearAllMocks();
    user = null;
    failure = null;
    TestBed.configureTestingModule({
      imports: [SsoLogin],
      providers: [
        provideRouter([]), ...provideI18nTesting('en', {
          en: { ...authenticationEn, ...accountEn },
          fr: { ...authenticationFr, ...accountFr },
        }),
        { provide: Auth, useValue: { user: () => user } },
        { provide: PublicSsoApi, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: {
          get queryParamMap() { return convertToParamMap(failure ? { ssoError: failure } : {}); },
        } } },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  it('shows active buttons and starts an external authorization flow, not a stored token', () => {
    const fixture = TestBed.createComponent(SsoLogin);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Company Okta');
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();
    expect(api.start).toHaveBeenCalledWith('okta');
    expect(api.navigate).toHaveBeenCalledWith('https://company.okta.com/authorize');
    expect(fixture.componentInstance.busy()).toBe('okta');
  });

  it('prevents duplicate starts while a request is in progress', () => {
    const pending = new Subject<{ authorizationUrl: string }>();
    api.start.mockReturnValueOnce(pending);
    const component = TestBed.createComponent(SsoLogin).componentInstance;
    component.login('okta');
    component.login('okta');
    expect(api.start).toHaveBeenCalledTimes(1);
    pending.error({ status: 429 });
    expect(component.busy()).toBeNull();
    expect(component.error()).toBe('rate_limited');
  });

  it('maps only fixed callback errors and never renders provider error text', () => {
    failure = 'email_collision';
    const component = TestBed.createComponent(SsoLogin).componentInstance;
    component.ngOnInit();
    expect(component.error()).toBe('email_collision');
    failure = 'arbitrary-sensitive-provider-description';
    component.ngOnInit();
    expect(component.error()).toBe('login_failed');
  });

  it('surfaces list and start failures safely', () => {
    api.providers.mockReturnValueOnce(throwError(() => ({ status: 503 })));
    api.start.mockReturnValueOnce(throwError(() => ({ status: 403, error: { secret: 'not-for-display' } })));
    const component = TestBed.createComponent(SsoLogin).componentInstance;
    component.ngOnInit();
    expect(component.loading()).toBe(false);
    expect(component.error()).toBe('request_failed');
    component.login('okta');
    expect(component.error()).toBe('login_failed');
  });

  it('keeps an existing ordinary session and navigates safely to the dashboard', () => {
    user = { role: 'User' };
    TestBed.createComponent(SsoLogin).componentInstance.ngOnInit();
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/dashboard');
    expect(api.providers).not.toHaveBeenCalled();
  });

  it('shows only a validated diagnostic reference and clears it on retry', () => {
    const reference = '1234567890abcdef1234567890abcdef';
    api.start.mockReturnValueOnce(throwError(() => ({
      status: 400, error: { error: 'login_failed', diagnosticId: reference, message: 'sensitive-provider-response' },
    })));
    const fixture = TestBed.createComponent(SsoLogin);
    fixture.detectChanges();
    fixture.componentInstance.login('okta');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(reference);
    expect(fixture.nativeElement.textContent).not.toContain('sensitive-provider-response');
    fixture.componentInstance.login('okta');
    expect(fixture.componentInstance.diagnosticId()).toBe('');
  });

  it('ignores untrusted diagnostic reference formats', () => {
    api.start.mockReturnValueOnce(throwError(() => ({
      status: 400, error: { diagnosticId: 'provider-response-with-sensitive-claims' },
    })));
    const component = TestBed.createComponent(SsoLogin).componentInstance;
    component.login('okta');
    expect(component.diagnosticId()).toBe('');
  });
});
