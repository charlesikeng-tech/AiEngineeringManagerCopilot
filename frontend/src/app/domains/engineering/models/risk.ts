import type { MetricType } from './metric';

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

export type RiskSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

export type RiskCategory = string;
