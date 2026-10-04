import { EnvironmentProviders, Provider } from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { enUS } from 'date-fns/locale';
import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';

export function provideFrontendI18n(): (Provider | EnvironmentProviders)[] {
  return [
    ...provideTranslateService({
      fallbackLang: 'en',
      loader: provideTranslateHttpLoader({
        resources: [
          './i18n/common/',
          './i18n/team/',
          './i18n/dashboard/',
          './i18n/management/',
          './i18n/authentication/',
          './i18n/account/',
          './i18n/administration/',
        ],
        failOnError: true,
      }),
    }),
    provideNzI18n(en_US),
    provideNzDateFnsAdapter({ locale: enUS, firstDayOfWeek: 1 }),
  ];
}
