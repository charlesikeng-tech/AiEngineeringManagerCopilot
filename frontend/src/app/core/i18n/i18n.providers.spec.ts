import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateLoader } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import commonEn from '../../../../public/i18n/common/en.json';
import commonFr from '../../../../public/i18n/common/fr.json';
import teamEn from '../../../../public/i18n/team/en.json';
import teamFr from '../../../../public/i18n/team/fr.json';
import dashboardEn from '../../../../public/i18n/dashboard/en.json';
import dashboardFr from '../../../../public/i18n/dashboard/fr.json';
import managementEn from '../../../../public/i18n/management/en.json';
import managementFr from '../../../../public/i18n/management/fr.json';
import authenticationEn from '../../../../public/i18n/authentication/en.json';
import authenticationFr from '../../../../public/i18n/authentication/fr.json';
import accountEn from '../../../../public/i18n/account/en.json';
import accountFr from '../../../../public/i18n/account/fr.json';
import administrationEn from '../../../../public/i18n/administration/en.json';
import administrationFr from '../../../../public/i18n/administration/fr.json';
import { provideFrontendI18n } from './i18n.providers';
import { I18nService, LANGUAGE_STORAGE_KEY } from './i18n.service';

const resources = {
  en: {
    common: commonEn,
    team: teamEn,
    dashboard: dashboardEn,
    management: managementEn,
    authentication: authenticationEn,
    account: accountEn,
    administration: administrationEn,
  },
  fr: {
    common: commonFr,
    team: teamFr,
    dashboard: dashboardFr,
    management: managementFr,
    authentication: authenticationFr,
    account: accountFr,
    administration: administrationFr,
  },
};

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

  it.each(['en', 'fr'] as const)(
    'loads every %s resource and deep-merges the shared sso namespace',
    async (language) => {
      const result = firstValueFrom(TestBed.inject(TranslateLoader).getTranslation(language));
      const http = TestBed.inject(HttpTestingController);
      const dictionaries = resources[language];

      for (const [scope, dictionary] of Object.entries(dictionaries)) {
        http.expectOne(`./i18n/${scope}/${language}.json`).flush(dictionary);
      }

      await expect(result).resolves.toEqual({
        ...dictionaries.common,
        ...dictionaries.team,
        ...dictionaries.dashboard,
        ...dictionaries.management,
        ...dictionaries.account,
        sso: { ...dictionaries.authentication.sso, ...dictionaries.administration.sso },
      });
    },
  );

  it.each(['common', 'authentication', 'account', 'administration'])(
    'surfaces a missing %s dictionary rather than silently serving partial translations',
    async (scope) => {
      const result = firstValueFrom(TestBed.inject(TranslateLoader).getTranslation('fr'));
      const rejection = expect(result).rejects.toMatchObject({ status: 404 });

      TestBed.inject(HttpTestingController)
        .expectOne(`./i18n/${scope}/fr.json`)
        .flush('Not found', { status: 404, statusText: 'Not Found' });

      await rejection;
    },
  );

  it('keeps the current language and preference when another language cannot load', async () => {
    const service = TestBed.inject(I18nService);
    const initialized = service.initialize();
    const http = TestBed.inject(HttpTestingController);

    for (const [scope, dictionary] of Object.entries(resources.en)) {
      http.expectOne(`./i18n/${scope}/en.json`).flush(dictionary);
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
