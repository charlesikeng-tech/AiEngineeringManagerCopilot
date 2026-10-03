import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateLoader } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import commonEn from '../../../../public/i18n/common/en.json';
import { provideFrontendI18n } from './i18n.providers';
import { I18nService, LANGUAGE_STORAGE_KEY } from './i18n.service';

describe('translation HTTP loader', () => {
  beforeEach(() => {
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['en-US']);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ...provideFrontendI18n()],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify({ ignoreCancelled: true });
    vi.restoreAllMocks();
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('loads and merges every feature dictionary for the selected language', async () => {
    const result = firstValueFrom(TestBed.inject(TranslateLoader).getTranslation('fr'));
    const http = TestBed.inject(HttpTestingController);

    for (const scope of ['common', 'team', 'dashboard', 'management', 'sso']) {
      http.expectOne(`./i18n/${scope}/fr.json`).flush({ [scope]: { label: scope } });
    }

    await expect(result).resolves.toEqual({
      common: { label: 'common' },
      team: { label: 'team' },
      dashboard: { label: 'dashboard' },
      management: { label: 'management' },
      sso: { label: 'sso' },
    });
  });

  it('surfaces a missing dictionary rather than silently serving partial translations', async () => {
    const result = firstValueFrom(TestBed.inject(TranslateLoader).getTranslation('fr'));
    const rejection = expect(result).rejects.toMatchObject({ status: 404 });

    TestBed.inject(HttpTestingController)
      .expectOne('./i18n/common/fr.json')
      .flush('Not found', { status: 404, statusText: 'Not Found' });

    await rejection;
  });

  it('keeps the current language and preference when another language cannot load', async () => {
    const service = TestBed.inject(I18nService);
    const initialized = service.initialize();
    const http = TestBed.inject(HttpTestingController);

    for (const scope of ['common', 'team', 'dashboard', 'management', 'sso']) {
      http.expectOne(`./i18n/${scope}/en.json`).flush(scope === 'common' ? commonEn : {});
    }
    await initialized;

    const switchLanguage = service.setLanguage('fr');
    const rejection = expect(switchLanguage).rejects.toMatchObject({ status: 404 });
    http
      .expectOne('./i18n/common/fr.json')
      .flush('Not found', { status: 404, statusText: 'Not Found' });
    await rejection;

    expect(service.language()).toBe('en');
    expect(service.t('app.reports')).toBe('Reports');
    expect(document.documentElement.lang).toBe('en');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en');
  });
});
