import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzIconModule } from 'ng-zorro-antd/icon';

import {
  EngineeringMetric,
  MetricDataStatus,
  MetricTrend,
  MetricTrendDirection,
  MetricType,
} from '../../models/engineering-dashboard-response';

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
  imports: [NzCardModule, NzEmptyModule, NzGridModule, NzIconModule],
  templateUrl: './engineering-metrics.html',
  styleUrl: './engineering-metrics.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EngineeringMetrics {
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
      return 'Not connected';
    }

    if (item.dataStatus === 'NoData' || item.value === null) {
      return 'No data';
    }

    return `${item.value}${item.suffix}`;
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
      return trend.direction;
    }

    const percentage = Math.abs(trend.changePercentage);

    return `${trend.direction} ${percentage.toFixed(1)}%`;
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
    const labels: Record<MetricType, string> = {
      CycleTime: 'Cycle Time',
      PRReviewTime: 'PR Review Time',
      DeploymentFrequency: 'Deployments',
      ChangeFailureRate: 'Change Failure Rate',
      LeadTime: 'Lead Time',
      OpenPRs: 'Open PRs',
      MergedPRs: 'Merged PRs',
      BlockedItems: 'Blocked Items',
    };

    return labels[metricType];
  }

  private getSuffix(metricType: MetricType): string {
    switch (metricType) {
      case 'CycleTime':
      case 'PRReviewTime':
      case 'LeadTime':
        return 'h';

      case 'ChangeFailureRate':
        return '%';

      default:
        return '';
    }
  }
}
