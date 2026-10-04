import type { AIAnalysis } from '@domains/engineering/models/ai-analysis';
import type {
  EngineeringHealthHistoryPoint,
  EngineeringHealthScore,
} from '@domains/engineering/models/health';
import type { EngineeringMetric, MetricTrend } from '@domains/engineering/models/metric';
import type { EngineeringReport } from '@domains/engineering/models/report';
import type { EngineeringRisk } from '@domains/engineering/models/risk';

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
