import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DecimalPipe } from '@angular/common';

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

  readonly cycleTime = computed(() => this.metricValue('CycleTime'));

  readonly deployments = computed(() => this.metricValue('DeploymentFrequency'));

  readonly cycleTimeTrend = computed(() => this.metricTrend('CycleTime'));

  readonly deploymentTrend = computed(() => this.metricTrend('DeploymentFrequency'));

  readonly highRiskCount = computed(
    () =>
      this.risks().filter((risk) => risk.severity === 'High' || risk.severity === 'Critical')
        .length,
  );

  trendPercentage(trend: MetricTrend | null): string {
    if (!trend?.changePercentage) {
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

  private metricValue(type: MetricType): number {
    return this.metrics().find((metric) => metric.metricType === type)?.value ?? 0;
  }

  private metricTrend(type: MetricType): MetricTrend | null {
    return this.trends().find((trend) => trend.metricType === type) ?? null;
  }
}
