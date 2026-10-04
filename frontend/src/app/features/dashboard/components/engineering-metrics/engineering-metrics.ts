import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18nService } from '@core/i18n/i18n.service';
import { TranslatePipe } from '@ngx-translate/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzIconModule } from 'ng-zorro-antd/icon';

import type {
  EngineeringMetric,
  MetricDataStatus,
  MetricTrend,
  MetricTrendDirection,
  MetricType,
} from '@domains/engineering/models/metric';

interface MetricViewModel {
  type: MetricType;
  label: string;
  value: number | null;
  suffix: string;
  dataStatus: MetricDataStatus;
  trend: MetricTrend | null;
}

@Component({
  selector: 'app-engineering-metrics',
  standalone: true,
  imports: [TranslatePipe, NzCardModule, NzEmptyModule, NzGridModule, NzIconModule],
  templateUrl: './engineering-metrics.html',
  styleUrl: './engineering-metrics.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EngineeringMetrics {
  private readonly i18n = inject(I18nService);
  readonly metrics = input.required<readonly EngineeringMetric[]>();
  readonly trends = input.required<readonly MetricTrend[]>();

  readonly items = computed<readonly MetricViewModel[]>(() =>
    this.metrics().map((metric) => ({
      type: metric.metricType,
      label: this.getLabel(metric.metricType),
      value: metric.value,
      suffix: this.getSuffix(metric.metricType),
      dataStatus: metric.dataStatus,
      trend:
        metric.dataStatus === 'Available'
          ? (this.trends().find((trend) => trend.metricType === metric.metricType) ?? null)
          : null,
    })),
  );

  displayValue(item: MetricViewModel): string {
    if (item.dataStatus === 'SourceNotConfigured') {
      return this.i18n.t('dashboard.notConnected');
    }

    if (item.dataStatus === 'NoData' || item.value === null) {
      return this.i18n.t('dashboard.noData');
    }

    if (item.type === 'ChangeFailureRate') {
      return new Intl.NumberFormat(this.i18n.locale(), {
        style: 'percent',
        maximumFractionDigits: 20,
      }).format(item.value / 100);
    }

    return `${new Intl.NumberFormat(this.i18n.locale(), { maximumFractionDigits: 20 }).format(item.value)}${item.suffix}`;
  }

  trendIcon(direction: MetricTrendDirection): string {
    switch (direction) {
      case 'Improving':
        return 'arrow-up';

      case 'Degrading':
        return 'arrow-down';

      case 'Stable':
        return 'minus';
    }
  }

  trendLabel(trend: MetricTrend): string {
    if (trend.changePercentage === null) {
      return this.i18n.t(`dashboard.trends.${trend.direction}`);
    }

    const percentage = Math.abs(trend.changePercentage);

    const formatted = new Intl.NumberFormat(this.i18n.locale(), {
      style: 'percent',
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    }).format(percentage / 100);
    return this.i18n.t('dashboard.trendChange', {
      direction: this.i18n.t(`dashboard.trends.${trend.direction}`),
      percentage: formatted,
    });
  }

  trendClass(direction: MetricTrendDirection): string {
    switch (direction) {
      case 'Improving':
        return 'trend-improving';

      case 'Degrading':
        return 'trend-degrading';

      case 'Stable':
        return 'trend-stable';
    }
  }

  private getLabel(metricType: MetricType): string {
    return this.i18n.t(`metrics.${metricType}`);
  }

  private getSuffix(metricType: MetricType): string {
    switch (metricType) {
      case 'CycleTime':
      case 'PRReviewTime':
      case 'LeadTime':
        return this.i18n.t('dashboard.hourUnit');

      case 'ChangeFailureRate':
        return '%';

      default:
        return '';
    }
  }
}
