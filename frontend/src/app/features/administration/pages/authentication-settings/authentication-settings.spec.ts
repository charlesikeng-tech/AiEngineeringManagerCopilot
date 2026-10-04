import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { of, Subject, throwError } from 'rxjs';
import en from '../../../../../../public/i18n/sso/en.json';
import fr from '../../../../../../public/i18n/sso/fr.json';
import { AuthenticationSettings } from './authentication-settings';
import { SsoApi, SsoProvider } from '../../data-access/sso-api';

describe('Authentication settings', () => {
  const provider: SsoProvider = {
    id: 'id', name: 'Test', type: 'Auth0', authority: 'https://example.auth0.com/', clientId: 'client',
    hasSecret: true, revision: 2, testedRevision: null, testedAt: null, lastTestError: null, activeRevision: 1,
    active: { name: 'Previous', type: 'Auth0', authority: 'https://previous.auth0.com/', clientId: 'client', hasSecret: true },
  };
  let api: { list: ReturnType<typeof vi.fn>; save: ReturnType<typeof vi.fn>; activate: ReturnType<typeof vi.fn> };
  let outcome: string | null;
  beforeEach(() => {
    outcome = null;
    api = {
      list: vi.fn(() => of({ callbackUrl: 'https://api.example/auth/sso/callback', providers: [provider] })),
      save: vi.fn(() => of(provider)),
      activate: vi.fn(() => of(provider)),
    };
    TestBed.configureTestingModule({
      imports: [AuthenticationSettings],
      providers: [
        provideRouter([]), ...provideI18nTesting('en', { en, fr }),
        { provide: SsoApi, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { get queryParamMap() { return convertToParamMap(outcome ? { ssoTest: outcome } : {}); } } } },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  it('retains secrets without preloading or echoing a masked value', () => {
    const component = TestBed.createComponent(AuthenticationSettings).componentInstance;
    component.edit(provider);
    expect(component.clientSecret).toBe('');
    expect(component.draft.secretAction).toBe('retain');
    component.save();
    expect(api.save).toHaveBeenCalledWith('id', expect.not.objectContaining({ clientSecret: expect.anything() }));
  });
  it('clears replacement credentials even on failure and keeps the editor usable', () => {
    api.save.mockReturnValueOnce(throwError(() => ({ error: { error: 'revision_conflict' } })));
    const component = TestBed.createComponent(AuthenticationSettings).componentInstance;
    component.edit(provider);
    component.draft.secretAction = 'replace';
    component.clientSecret = 'never-persist';
    component.save();
    expect(api.save).toHaveBeenCalledWith('id', expect.objectContaining({ clientSecret: 'never-persist', secretAction: 'replace' }));
    expect(component.clientSecret).toBe('');
    expect(component.pending()).toBe(false);
    expect(component.editing()).toBe(true);
    expect(component.error()).toBe('sso.errors.revision_conflict');
    expect(component.providers()[0].active?.name).toBe('Previous');
  });
  it('blocks duplicate requests and disables activation until verification', () => {
    const save = new Subject<SsoProvider>();
    api.save.mockReturnValueOnce(save);
    const fixture = TestBed.createComponent(AuthenticationSettings);
    fixture.detectChanges();
    const activation = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .find((button) => button.textContent?.includes('Activate verified'));
    expect(activation?.disabled).toBe(true);
    fixture.componentInstance.edit(provider);
    fixture.componentInstance.save();
    fixture.componentInstance.save();
    expect(api.save).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.pending()).toBe(true);
    save.next(provider);
    save.complete();
    expect(fixture.componentInstance.pending()).toBe(false);
  });
  it('surfaces initial load failure and allows retry', () => {
    api.list.mockReturnValueOnce(throwError(() => ({ status: 503 })));
    const component = TestBed.createComponent(AuthenticationSettings).componentInstance;
    expect(component.loaded()).toBe(false);
    expect(component.error()).toBe('sso.errors.request_failed');
    component.load();
    expect(component.loaded()).toBe(true);
  });
  it('treats callback query as feedback only and loads authoritative server status', () => {
    outcome = 'success';
    const component = TestBed.createComponent(AuthenticationSettings).componentInstance;
    expect(component.feedback()).toBe('sso.testSuccess');
    expect(component.providers()[0].testedRevision).toBeNull();
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith([], expect.objectContaining({ replaceUrl: true, queryParams: {} }));
  });
  it('has matching English/French keys and explicitly describes ordinary-user login scope', () => {
    function keys(value: object, prefix = ''): string[] {
      return Object.entries(value).flatMap(([key, child]) => typeof child === 'object' ? keys(child, `${prefix}${key}.`) : [`${prefix}${key}`]).sort();
    }
    expect(keys(en)).toEqual(keys(fr));
    expect(en.sso.scope).toContain('never grants administrator privileges');
    expect(fr.sso.scope).toContain('jamais un administrateur');
  });
});
