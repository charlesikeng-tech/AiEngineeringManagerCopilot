import type { ActionPriority } from './action';
import type { MetricType } from './metric';

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
