import { PagedResult } from '@core/models/paged-result';
import type { EngineeringRisk } from '@domains/engineering/models/risk';

export interface PagedRisksResponse {
  teamId: string;
  reportId: string;
  periodStart: string;
  periodEnd: string;
  page: PagedResult<EngineeringRisk>;
  summary: {
    criticalCount: number;
    highCount: number;
    mediumCount: number;
    lowCount: number;
  };
}
