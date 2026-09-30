import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzIconModule } from 'ng-zorro-antd/icon';

import {
  EngineeringHealthScore,
  EngineeringMetric,
  EngineeringRisk,
  MetricTrend,
  MetricType,
} from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-dashboard-kpis',
  standalone: true,
  imports: [DecimalPipe, NzCardModule, NzGridModule, NzIconModule],
  templateUrl: './dashboard-kpis.html',
  styleUrl: './dashboard-kpis.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardKpis {
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
      return 'No data';
    }

    if (metric.dataStatus === 'SourceNotConfigured') {
      return 'Not connected';
    }

    if (metric.dataStatus === 'NoData' || metric.value === null) {
      return 'No data';
    }

    return metric.value.toString();
  }

  isAvailable(metric: EngineeringMetric | null): boolean {
    return metric?.dataStatus === 'Available' && metric.value !== null;
  }

  trendPercentage(trend: MetricTrend | null): string {
    if (trend?.changePercentage == null) {
      return '0%';
    }

    return `${Math.abs(trend.changePercentage).toFixed(1)}%`;
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
