import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';

import type { EChartsOption } from 'echarts';
import { NgxEchartsDirective, provideEchartsCore } from 'ngx-echarts';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';

import type { EngineeringHealthHistoryPoint } from '@domains/engineering/models/health';

@Component({
  selector: 'app-health-evolution',
  standalone: true,
  imports: [TranslatePipe, LocalizedNumberPipe, NgxEchartsDirective, NzCardModule, NzEmptyModule],
  providers: [
    provideEchartsCore({
      echarts: () => import('../../echarts/echarts-core'),
    }),
  ],
  templateUrl: './health-evolution.html',
  styleUrl: './health-evolution.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthEvolution {
  private readonly i18n = inject(I18nService);
  readonly history = input.required<readonly EngineeringHealthHistoryPoint[]>();

  readonly chartOptions = computed<EChartsOption>(() => {
    this.i18n.language();
    const history = this.history();
    const numbers = new Intl.NumberFormat(this.i18n.locale(), { maximumFractionDigits: 20 });
    const percentages = new Intl.NumberFormat(this.i18n.locale(), {
      style: 'percent',
      maximumFractionDigits: 0,
    });

    return {
      animationDuration: 500,

      tooltip: {
        trigger: 'axis',
        formatter: (params) => {
          const item = Array.isArray(params) ? params[0] : params;

          if (!item) {
            return '';
          }

          const point = history[item.dataIndex];

          if (!point) {
            return '';
          }

          return `
            <strong>${this.formatPeriod(point)}</strong><br />
            ${this.i18n.t('dashboard.healthScore')}: <strong>${numbers.format(point.overallScore)}/100</strong><br />
            ${this.i18n.t(`healthLevels.${point.healthLevel}`)}<br />
            ${this.i18n.t('dashboard.dataCoverage')}: ${percentages.format(Math.round(point.dataCoverage) / 100)}
          `;
        },
      },

      grid: {
        left: 12,
        right: 12,
        top: 24,
        bottom: 8,
        outerBounds: {
          left: 0,
          right: 0,
          top: 0,
          bottom: 0,
        },
      },

      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: history.map((point) => this.formatPeriod(point)),
        axisLine: {
          lineStyle: {
            color: '#eaecf0',
          },
        },
        axisTick: {
          show: false,
        },
        axisLabel: {
          color: '#98a2b3',
          fontSize: 11,
        },
      },

      yAxis: {
        type: 'value',
        min: 0,
        max: 100,
        interval: 25,
        axisLine: {
          show: false,
        },
        axisTick: {
          show: false,
        },
        axisLabel: {
          color: '#98a2b3',
          fontSize: 11,
          formatter: (value: number) => numbers.format(value),
        },
        splitLine: {
          lineStyle: {
            color: '#f2f4f7',
          },
        },
      },

      series: [
        {
          name: this.i18n.t('dashboard.healthScore'),
          type: 'line',
          smooth: true,
          symbol: 'circle',
          symbolSize: 8,
          showSymbol: true,

          lineStyle: {
            width: 3,
            color: '#4f46e5',
          },

          itemStyle: {
            color: '#4f46e5',
            borderColor: '#ffffff',
            borderWidth: 2,
          },

          areaStyle: {
            color: 'rgba(79, 70, 229, 0.08)',
          },

          data: history.map((point) => point.overallScore),
        },
      ],
    };
  });

  private formatPeriod(point: EngineeringHealthHistoryPoint): string {
    const start = new Date(`${point.periodStart}T00:00:00`);

    return new Intl.DateTimeFormat(this.i18n.locale(), {
      month: 'short',
      year: 'numeric',
    }).format(start);
  }
}
