import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Auth } from '@core/auth/auth';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { of, Subject, throwError } from 'rxjs';
import en from '../../../../../../public/i18n/account/en.json';
import fr from '../../../../../../public/i18n/account/fr.json';
import { AccountLinkApi } from '../../data-access/account-link-api';
import { AccountSecurity } from './account-security';

describe('Account security', () => {
  let role: string;
  let outcome: string | null;
  let api: {
    list: ReturnType<typeof vi.fn>; providers: ReturnType<typeof vi.fn>; start: ReturnType<typeof vi.fn>;
    unlink: ReturnType<typeof vi.fn>; navigate: ReturnType<typeof vi.fn>;
  };
  beforeEach(() => {
    role = 'PlatformAdministrator';
    outcome = null;
    api = {
      list: vi.fn(() => of({ canLink: true, identities: [] })),
      providers: vi.fn(() => of([{ id: 'okta', name: 'Current active Okta', type: 'Okta' }])),
      start: vi.fn(() => of({ authorizationUrl: 'https://company.okta.com/authorize' })),
      unlink: vi.fn(() => of(undefined)),
      navigate: vi.fn(),
    };
    TestBed.configureTestingModule({
      imports: [AccountSecurity],
      providers: [
        provideRouter([]), ...provideI18nTesting('en', { en, fr }),
        { provide: Auth, useValue: { user: () => ({ role }) } },
        { provide: AccountLinkApi, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: {
          get queryParamMap() { return convertToParamMap(outcome ? { link: outcome } : {}); },
        } } },
      ],
    });
  });

  it('renders current active provider and requires fresh password and explicit consent', () => {
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    // nz-select only renders its options once opened.
    expect(component.providers().map((provider) => provider.name)).toEqual(['Current active Okta']);
    expect((fixture.nativeElement.querySelector('#password') as HTMLInputElement).type).toBe('password');
    component.providerId = 'okta';
    component.password = 'ephemeral';
    component.start();
    expect(api.start).not.toHaveBeenCalled();
    component.consent = true;
    component.start();
    expect(api.start).toHaveBeenCalledWith('okta', 'ephemeral', true);
    expect(component.password).toBe('');
    expect(api.navigate).toHaveBeenCalledWith('https://company.okta.com/authorize');
  });

  it('blocks duplicate submissions while pending and safely handles confirmation errors', () => {
    const pending = new Subject<{ authorizationUrl: string }>();
    api.start.mockReturnValueOnce(pending);
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.providerId = 'okta';
    component.password = 'ephemeral';
    component.consent = true;
    component.start();
    component.start();
    expect(api.start).toHaveBeenCalledTimes(1);
    pending.error({ status: 401, error: { description: 'sensitive body' } });
    fixture.detectChanges();
    expect(component.busy()).toBe(false);
    expect(component.error()).toBe('confirmation_failed');
    expect(fixture.nativeElement.textContent).not.toContain('sensitive body');
  });

  it('does not accept a cached SSO administrator profile as local proof', () => {
    api.list.mockReturnValueOnce(of({ canLink: false, identities: [] }));
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.providerId = 'okta';
    component.password = 'anything';
    component.consent = true;
    component.start();
    expect(api.start).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe('/admin/login');
  });

  it('offers no linking action to ordinary accounts', () => {
    role = 'User';
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    expect(api.list).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('only for existing local administrators');
  });

  it('treats return query as feedback only and loads authoritative linkage state', () => {
    outcome = 'success';
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    expect(api.list).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.state()?.identities).toEqual([]);
    expect(api.start).not.toHaveBeenCalled();
    outcome = 'raw-provider-secrets';
    fixture.componentInstance.ngOnInit();
    expect(fixture.componentInstance.outcome()).toBe('link_failed');
  });

  it('unlinks only with a new password, clears it, and refreshes authoritative list', () => {
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.unlink('identity');
    expect(api.unlink).not.toHaveBeenCalled();
    component.password = 'fresh-password';
    component.unlink('identity');
    expect(api.unlink).toHaveBeenCalledWith('identity', 'fresh-password');
    expect(component.password).toBe('');
    expect(api.list).toHaveBeenCalledTimes(2);
    expect(component.outcome()).toBe('unlinked');
  });

  it('never navigates after a failed authorization URL validation', () => {
    api.navigate.mockImplementationOnce(() => { throw new Error('Invalid URL'); });
    const fixture = TestBed.createComponent(AccountSecurity);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.providerId = 'okta';
    component.password = 'password';
    component.consent = true;
    component.start();
    expect(component.error()).toBe('request_failed');
    expect(component.busy()).toBe(false);
    api.list.mockReturnValueOnce(throwError(() => ({ status: 503 })));
    component.load();
    expect(component.state()).toBeNull();
  });

  it('has complete matching English and French account-link dictionaries', () => {
    function keys(value: object, prefix = ''): string[] {
      return Object.entries(value).flatMap(([key, child]) => typeof child === 'object'
        ? keys(child, `${prefix}${key}.`) : [`${prefix}${key}`]).sort();
    }
    expect(keys(en.accountLink)).toEqual(keys(fr.accountLink));
    expect(en.accountLink.consent).toContain('administrator access');
    expect(fr.accountLink.consent).toContain('accès administrateur');
  });
});
