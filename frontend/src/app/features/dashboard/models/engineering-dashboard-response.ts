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
  value: number;
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

export interface EngineeringReport {
  id: string;
  teamId: string;
  periodStart: string;
  periodEnd: string;
  executiveSummary: string;
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
  createdAt: string;
}

export interface AIAnalysis {
  summary: string;
  insights: LlmInsight[];
  actions: LlmAction[];
  evidence: LlmEvidence[] | null;
}

export interface LlmInsight {
  title: string;
  description: string;
}

export interface LlmAction {
  title: string;
  description: string;
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

export type MetricTrendDirection = 'Improving' | 'Stable' | 'Degrading';

export type RiskSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

export type RiskCategory = string;
