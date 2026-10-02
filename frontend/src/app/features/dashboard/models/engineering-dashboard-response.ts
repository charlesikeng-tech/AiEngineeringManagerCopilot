export interface EngineeringDashboardResponse {
  teamId: string;
  metrics: EngineeringMetric[];
  healthScore: EngineeringHealthScore;
  trends: MetricTrend[];
  risks: EngineeringRisk[];
  healthHistory: EngineeringHealthHistoryPoint[];
  latestReport: EngineeringReport | null;
  aiAnalysis: AIAnalysis | null;
}

export interface EngineeringReport {
  hasSnapshot?: boolean;
  id: string;
  teamId: string;
  periodStart: string;
  periodEnd: string;
  executiveSummary: string;
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
  createdAt: string;

  metrics: EngineeringReportMetric[];
  insights: EngineeringReportInsight[];
  actions: EngineeringReportAction[];
  risks: EngineeringReportRisk[];
  trends: EngineeringReportTrend[];
}

export interface EngineeringReportMetric {
  metricType: MetricType;
  value: number;
  score: number;
}

export interface EngineeringReportInsight {
  id: string;
  reportId: string;
  metricType: string;
  category: string;
  title: string;
  description: string;
  impact: string;
  recommendation: string;
}

export interface EngineeringReportAction {
  id: string;
  reportId: string;
  metricType: string | null;
  title: string;
  description: string;
  priority: string;
  owner: string | null;
  dueDate: string | null;
  status: string;
  createdAt: string;
}

export interface EngineeringReportRisk {
  id: string;
  reportId: string;
  severity: string;
  category: string;
  title: string;
  description: string;
  recommendation: string;
  createdAt: string;
}

export interface EngineeringReportTrend {
  metricType: string;
  currentValue: number;
  previousValue: number;
  changePercentage: number | null;
  direction: string;
}

export interface EngineeringHealthHistoryPoint {
  periodStart: string;
  periodEnd: string;
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
}

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

export interface EngineeringHealthScore {
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
}

export interface MetricTrend {
  metricType: MetricType;
  currentValue: number;
  previousValue: number;
  changePercentage: number | null;
  direction: MetricTrendDirection;
}

export interface EngineeringRisk {
  id: string;
  reportId: string;
  metricType: MetricType | null;
  severity: RiskSeverity;
  category: RiskCategory;
  title: string;
  description: string;
  recommendation: string;
  createdAt: string;
}

export interface EngineeringRisksResponse {
  teamId: string;
  reportId: string;
  periodStart: string;
  periodEnd: string;
  risks: EngineeringRisk[];
}

export interface EngineeringAction {
  id: string;
  reportId: string;
  metricType: string | null;
  title: string;
  description: string;
  priority: ActionPriority;
  owner: string | null;
  dueDate: string | null;
  status: ActionStatus;
  createdAt: string;
}

export interface EngineeringActionsResponse {
  teamId: string;
  reportId: string;
  periodStart: string;
  periodEnd: string;
  actions: EngineeringAction[];
}

export interface UpdateEngineeringActionRequest {
  status?: ActionStatus;
  owner?: string | null;
  dueDate?: string | null;
}

export type ActionStatus = 'Todo' | 'InProgress' | 'Done' | 'Cancelled';

export type ActionPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export interface AIAnalysis {
  summary: string;
  insights: LlmInsight[];
  actions: LlmAction[];
  evidence: LlmEvidence[] | null;
  safeEvidence: LlmEvidence[] | null;
}

export interface LlmInsight {
  category: string;
  title: string;
  description: string;
  impact: string;
  recommendation: string;
}

export interface LlmAction {
  title: string;
  description: string;
  priority: ActionPriority;
}

export interface LlmEvidence {
  metricType: MetricType;
  value: number;
  reason: string;
  confidence: number;
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

export type RiskSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

export type RiskCategory = string;
