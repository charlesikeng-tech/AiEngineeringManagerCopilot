import { PagedResult } from '@core/models/paged-result';
import { EngineeringRisk } from '@features/dashboard/models/engineering-dashboard-response';

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
