import { EnvironmentProviders, Provider } from '@angular/core';
import { provideTranslateService, TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { enUS } from 'date-fns/locale';
import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { of } from 'rxjs';

import commonEn from '../../../../public/i18n/common/en.json';
import commonFr from '../../../../public/i18n/common/fr.json';
import { AppLanguage, isAppLanguage } from './i18n.service';

export function provideI18nTesting(
  language: AppLanguage = 'en',
  extra?: Record<AppLanguage, TranslationObject>,
): (Provider | EnvironmentProviders)[] {
  const dictionaries: Record<AppLanguage, TranslationObject> = {
    en: { ...commonEn, ...extra?.en },
    fr: { ...commonFr, ...extra?.fr },
  };

  return [
    ...provideTranslateService({
      lang: language,
      fallbackLang: 'en',
      loader: {
        provide: TranslateLoader,
        useValue: {
          getTranslation(lang: string) {
            if (!isAppLanguage(lang)) {
              throw new Error(`Unsupported test language: ${lang}`);
            }
            return of(dictionaries[lang]);
          },
        },
      },
    }),
    provideNzI18n(en_US),
    provideNzDateFnsAdapter({ locale: enUS, firstDayOfWeek: 1 }),
  ];
}
