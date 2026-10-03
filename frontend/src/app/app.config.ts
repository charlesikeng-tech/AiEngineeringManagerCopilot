import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter } from '@angular/router';

import {
  ArrowDownOutline,
  ArrowUpOutline,
  BarChartOutline,
  CheckSquareOutline,
  DashboardOutline,
  FileTextOutline,
  GithubOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  MinusOutline,
  PlusOutline,
  SlackOutline,
  SyncOutline,
  TeamOutline,
  WarningOutline,
} from '@ant-design/icons-angular/icons';

import { provideNzIcons } from 'ng-zorro-antd/icon';

import { firstValueFrom } from 'rxjs';

import { AppInitializer } from '@core/app/app-initializer';
import { authInterceptor } from '@core/auth/auth-interceptor';
import { provideFrontendI18n } from '@core/i18n/i18n.providers';
import { I18nService } from '@core/i18n/i18n.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(withInterceptors([authInterceptor])),

    ...provideFrontendI18n(),

    provideNzIcons([
      DashboardOutline,
      FileTextOutline,
      WarningOutline,
      CheckSquareOutline,
      TeamOutline,
      MenuFoldOutline,
      MenuUnfoldOutline,
      ArrowUpOutline,
      ArrowDownOutline,
      MinusOutline,
      PlusOutline,
      GithubOutline,
      SlackOutline,
      BarChartOutline,
      SyncOutline,
    ]),

    provideAppInitializer(() => {
      const initializer = inject(AppInitializer);
      const i18n = inject(I18nService);
      return Promise.all([i18n.initialize(), firstValueFrom(initializer.initialize())]);
    }),
  ],
};
