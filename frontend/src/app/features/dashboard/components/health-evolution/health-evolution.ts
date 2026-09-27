import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { EChartsOption } from 'echarts';
import { NgxEchartsDirective } from 'ngx-echarts';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';

import { EngineeringHealthHistoryPoint } from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-health-evolution',
  standalone: true,
  imports: [NgxEchartsDirective, NzCardModule, NzEmptyModule],
  templateUrl: './health-evolution.html',
  styleUrl: './health-evolution.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthEvolution {
  readonly history = input.required<readonly EngineeringHealthHistoryPoint[]>();

  readonly chartOptions = computed<EChartsOption>(() => {
    const history = this.history();

    return {
      animationDuration: 500,

      tooltip: {
        trigger: 'axis',
        formatter: (params: any) => {
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
            Health score: <strong>${point.overallScore}/100</strong><br />
            ${point.healthLevel}<br />
            Data coverage: ${Math.round(point.dataCoverage)}%
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
        },
        splitLine: {
          lineStyle: {
            color: '#f2f4f7',
          },
        },
      },

      series: [
        {
          name: 'Health Score',
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

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      year: 'numeric',
    }).format(start);
  }
}
