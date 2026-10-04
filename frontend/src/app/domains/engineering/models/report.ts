import type { MetricType } from './metric';

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
