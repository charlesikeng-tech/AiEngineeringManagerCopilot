import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter } from '@angular/router';

import {
  ArrowDownOutline,
  ArrowUpOutline,
  CheckSquareOutline,
  DashboardOutline,
  FileTextOutline,
  GithubOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  MinusOutline,
  PlusOutline,
  TeamOutline,
  WarningOutline,
} from '@ant-design/icons-angular/icons';

import { enUS } from 'date-fns/locale';

import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';

import { LineChart } from 'echarts/charts';
import { GridComponent, TooltipComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';

import { provideEchartsCore } from 'ngx-echarts';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { AppInitializer } from '@core/app/app-initializer';
import { authInterceptor } from '@core/auth/auth-interceptor';

echarts.use([LineChart, GridComponent, TooltipComponent, CanvasRenderer]);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(withInterceptors([authInterceptor])),

    provideNzI18n(en_US),

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
    ]),

    provideNzDateFnsAdapter({
      locale: enUS,
      firstDayOfWeek: 1,
    }),

    provideEchartsCore({
      echarts,
    }),

    provideAppInitializer(() => {
      const initializer = inject(AppInitializer);

      return firstValueFrom(initializer.initialize());
    }),
  ],
};
