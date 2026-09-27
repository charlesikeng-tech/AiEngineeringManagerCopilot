import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter } from '@angular/router';
import {
  ArrowDownOutline,
  ArrowUpOutline,
  DashboardOutline,
  FileTextOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  MinusOutline,
  TeamOutline,
  WarningOutline,
} from '@ant-design/icons-angular/icons';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { firstValueFrom } from 'rxjs';
import { routes } from './app.routes';
import { AppInitializer } from './core/app/app-initializer';
import { authInterceptor } from './core/auth/auth-interceptor';

import * as echarts from 'echarts/core';
import { provideEchartsCore } from 'ngx-echarts';

import { LineChart } from 'echarts/charts';

import { GridComponent, TooltipComponent } from 'echarts/components';

import { CanvasRenderer } from 'echarts/renderers';

echarts.use([LineChart, GridComponent, TooltipComponent, CanvasRenderer]);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(withInterceptors([authInterceptor])),

    provideNzIcons([
      DashboardOutline,
      FileTextOutline,
      WarningOutline,
      TeamOutline,
      MenuFoldOutline,
      MenuUnfoldOutline,
      ArrowUpOutline,
      ArrowDownOutline,
      MinusOutline,
    ]),
    provideEchartsCore({ echarts }),

    provideAppInitializer(() => {
      const initializer = inject(AppInitializer);

      return firstValueFrom(initializer.initialize());
    }),
  ],
};
