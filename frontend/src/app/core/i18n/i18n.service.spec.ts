import { TestBed } from '@angular/core/testing';
import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { NzDateAdapter } from 'ng-zorro-antd/core/time';
import { NzI18nService } from 'ng-zorro-antd/i18n';
import { of, Subject } from 'rxjs';

import commonEn from '../../../../public/i18n/common/en.json';
import commonFr from '../../../../public/i18n/common/fr.json';
import { provideI18nTesting } from './i18n-testing';
import { I18nService, isAppLanguage, LANGUAGE_STORAGE_KEY } from './i18n.service';

describe('I18nService', () => {
  beforeEach(() => {
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['en-US']);
    TestBed.configureTestingModule({ providers: provideI18nTesting() });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
    document.documentElement.lang = 'en';
  });

  it('restores the saved language before considering the browser language', async () => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'fr');
    const service = TestBed.inject(I18nService);

    await service.initialize();

    expect(service.language()).toBe('fr');
    expect(service.locale()).toBe('fr-FR');
    expect(service.t('app.reports')).toBe('Rapports');
    expect(document.documentElement.lang).toBe('fr');
    expect(document.title).toBe(commonFr.app.documentTitle);
  });

  it('detects French from the browser when no preference is saved', async () => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['fr-CA']);
    const service = TestBed.inject(I18nService);

    await service.initialize();

    expect(service.language()).toBe('fr');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('fr');
  });

  it('uses English for unsupported browser languages and warns about invalid saved preferences', async () => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'de');
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['de-DE']);
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const service = TestBed.inject(I18nService);

    await service.initialize();

    expect(service.language()).toBe('en');
    expect(warn).toHaveBeenCalledWith('Ignoring unsupported saved language: de');
  });

  it('switches text, UI locale, date adapter and saved preference together', async () => {
    const service = TestBed.inject(I18nService);
    const dateAdapter = TestBed.inject(NzDateAdapter);
    const date = new Date(2026, 8, 1);

    await service.setLanguage('fr');

    expect(service.t('app.teams')).toBe('Équipes');
    expect(TestBed.inject(NzI18nService).getLocaleId()).toBe('fr');
    expect(dateAdapter.format(date, 'MMMM')).toBe('septembre');
    expect(service.formatNumber(1234.5)).toContain('234,5');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('fr');

    await service.setLanguage('en');

    expect(service.t('app.teams')).toBe('Teams');
    expect(dateAdapter.format(date, 'MMMM')).toBe('September');
    expect(document.documentElement.lang).toBe('en');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en');
  });

  it('keeps language switching usable when browser storage is unavailable', async () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Storage is blocked', 'SecurityError');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is full', 'QuotaExceededError');
    });
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const service = TestBed.inject(I18nService);

    await service.initialize();
    await service.setLanguage('fr');

    expect(service.language()).toBe('fr');
    expect(service.t('app.reports')).toBe('Rapports');
    expect(warn).toHaveBeenCalled();
  });

  it('does not persist a superseded language request that completes late', async () => {
    const french = new Subject<TranslationObject>();
    TestBed.overrideProvider(TranslateLoader, {
      useValue: {
        getTranslation: (language: string) => (language === 'fr' ? french : of(commonEn)),
      },
    });
    const service = TestBed.inject(I18nService);

    const pendingFrench = service.setLanguage('fr');
    await service.setLanguage('en');
    french.next(commonFr);
    french.complete();
    await pendingFrench;

    expect(service.language()).toBe('en');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('recognizes only the supported application language codes', () => {
    expect(isAppLanguage('fr')).toBe(true);
    expect(isAppLanguage('en')).toBe(true);
    expect(isAppLanguage('fr-FR')).toBe(false);
    expect(isAppLanguage(null)).toBe(false);
  });
});
