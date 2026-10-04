export interface EngineeringMetric {
  id: string;
  teamId: string;
  metricType: MetricType;
  value: number | null;
  dataStatus: MetricDataStatus;
  periodStart: string;
  periodEnd: string;
  createdAt: string;
}

export interface MetricTrend {
  metricType: MetricType;
  currentValue: number;
  previousValue: number;
  changePercentage: number | null;
  direction: MetricTrendDirection;
}

export type MetricType =
  | 'CycleTime'
  | 'PRReviewTime'
  | 'DeploymentFrequency'
  | 'ChangeFailureRate'
  | 'LeadTime'
  | 'OpenPRs'
  | 'MergedPRs'
  | 'BlockedItems';

export type MetricDataStatus = 'Available' | 'NoData' | 'SourceNotConfigured';

export type MetricTrendDirection = 'Improving' | 'Stable' | 'Degrading';
