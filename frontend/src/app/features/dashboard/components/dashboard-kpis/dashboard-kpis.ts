import { LocalizedNumberPipe, LocalizedPercentPipe } from '@core/i18n/localized-format.pipes';
import { I18nService } from '@core/i18n/i18n.service';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzIconModule } from 'ng-zorro-antd/icon';

import type { EngineeringHealthScore } from '@domains/engineering/models/health';
import type {
  EngineeringMetric,
  MetricTrend,
  MetricType,
} from '@domains/engineering/models/metric';
import type { EngineeringRisk } from '@domains/engineering/models/risk';

@Component({
  selector: 'app-dashboard-kpis',
  standalone: true,
  imports: [TranslatePipe, LocalizedNumberPipe, LocalizedPercentPipe, NzCardModule, NzGridModule, NzIconModule],
  templateUrl: './dashboard-kpis.html',
  styleUrl: './dashboard-kpis.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardKpis {
  private readonly i18n = inject(I18nService);
  readonly healthScore = input.required<EngineeringHealthScore>();
  readonly metrics = input.required<readonly EngineeringMetric[]>();
  readonly trends = input.required<readonly MetricTrend[]>();
  readonly risks = input.required<readonly EngineeringRisk[]>();

  readonly cycleTimeMetric = computed(() => this.metric('CycleTime'));

  readonly deploymentMetric = computed(() => this.metric('DeploymentFrequency'));

  readonly cycleTimeTrend = computed(() => this.metricTrend('CycleTime'));

  readonly deploymentTrend = computed(() => this.metricTrend('DeploymentFrequency'));

  readonly highRiskCount = computed(
    () =>
      this.risks().filter((risk) => risk.severity === 'High' || risk.severity === 'Critical')
        .length,
  );

  readonly healthStatusClass = computed(() => {
    switch (this.healthScore().healthLevel) {
      case 'Excellent':
      case 'Healthy':
        return 'status-healthy';

      case 'Needs Attention':
        return 'status-warning';

      default:
        return 'status-danger';
    }
  });

  metricDisplay(metric: EngineeringMetric | null): string {
    if (!metric) {
      return this.i18n.t('dashboard.noData');
    }

    if (metric.dataStatus === 'SourceNotConfigured') {
      return this.i18n.t('dashboard.notConnected');
    }

    if (metric.dataStatus === 'NoData' || metric.value === null) {
      return this.i18n.t('dashboard.noData');
    }

    return new Intl.NumberFormat(this.i18n.locale(), { maximumFractionDigits: 20 }).format(metric.value);
  }

  isAvailable(metric: EngineeringMetric | null): boolean {
    return metric?.dataStatus === 'Available' && metric.value !== null;
  }

  trendPercentage(trend: MetricTrend | null): string {
    return new Intl.NumberFormat(this.i18n.locale(), {
      style: 'percent',
      minimumFractionDigits: trend?.changePercentage == null ? 0 : 1,
      maximumFractionDigits: 1,
    }).format(Math.abs(trend?.changePercentage ?? 0) / 100);
  }

  trendIcon(trend: MetricTrend | null): string {
    if (!trend || trend.direction === 'Stable') {
      return 'minus';
    }

    return trend.direction === 'Improving' ? 'arrow-up' : 'arrow-down';
  }

  private metric(type: MetricType): EngineeringMetric | null {
    return this.metrics().find((metric) => metric.metricType === type) ?? null;
  }

  private metricTrend(type: MetricType): MetricTrend | null {
    return this.trends().find((trend) => trend.metricType === type) ?? null;
  }
}
