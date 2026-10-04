import { PagedResult } from '@core/models/paged-result';
import type { EngineeringAction } from '@domains/engineering/models/action';

export interface PagedActionsResponse {
  teamId: string;
  reportId: string;
  periodStart: string;
  periodEnd: string;
  page: PagedResult<EngineeringAction>;
  summary: {
    todoCount: number;
    inProgressCount: number;
    doneCount: number;
    cancelledCount: number;
    overdueCount: number;
    asOfDate: string;
  };
}
