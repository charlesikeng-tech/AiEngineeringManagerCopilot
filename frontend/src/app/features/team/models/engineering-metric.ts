export type MetricDataStatus = 'Available' | 'NoData' | 'SourceNotConfigured';

export type MetricType =
  | 'CycleTime'
  | 'PRReviewTime'
  | 'DeploymentFrequency'
  | 'ChangeFailureRate'
  | 'LeadTime'
  | 'OpenPRs'
  | 'MergedPRs'
  | 'BlockedItems';

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
