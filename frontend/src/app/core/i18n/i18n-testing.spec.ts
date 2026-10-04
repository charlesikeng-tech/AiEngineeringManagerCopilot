import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import authenticationEn from '../../../../public/i18n/authentication/en.json';
import authenticationFr from '../../../../public/i18n/authentication/fr.json';
import accountEn from '../../../../public/i18n/account/en.json';
import accountFr from '../../../../public/i18n/account/fr.json';
import administrationEn from '../../../../public/i18n/administration/en.json';
import administrationFr from '../../../../public/i18n/administration/fr.json';
import { provideI18nTesting } from './i18n-testing';

describe('translation test fixtures', () => {
  it.each(['en', 'fr'] as const)(
    'resolves split %s resources without losing nested defaults',
    async (language) => {
      TestBed.configureTestingModule({
        providers: provideI18nTesting(language, {
          en: {
            ...accountEn,
            sso: { ...authenticationEn.sso, ...administrationEn.sso },
            app: { fixture: 'Fixture' },
          },
          fr: {
            ...accountFr,
            sso: { ...authenticationFr.sso, ...administrationFr.sso },
            app: { fixture: 'Exemple' },
          },
        }),
      });
      const translate = TestBed.inject(TranslateService);
      await firstValueFrom(translate.use(language));

      const authentication = language === 'en' ? authenticationEn : authenticationFr;
      const account = language === 'en' ? accountEn : accountFr;
      const administration = language === 'en' ? administrationEn : administrationFr;
      expect(translate.instant('sso.login.title')).toBe(authentication.sso.login.title);
      expect(translate.instant('sso.title')).toBe(administration.sso.title);
      expect(translate.instant('accountLink.title')).toBe(account.accountLink.title);
      expect(translate.instant('app.reports')).toBe(language === 'en' ? 'Reports' : 'Rapports');
      expect(translate.instant('app.fixture')).toBe(language === 'en' ? 'Fixture' : 'Exemple');
    },
  );
});
