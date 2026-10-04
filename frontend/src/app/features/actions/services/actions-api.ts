import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { PagedActionsResponse } from '../models/paged-actions-response';
import {
  EngineeringAction,
  EngineeringActionsResponse,
  UpdateEngineeringActionRequest,
} from '@features/dashboard/models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class ActionsApi {
  private readonly http = inject(HttpClient);

  getActionsPage(
    teamId: string,
    pageNumber: number,
    pageSize: number,
  ): Observable<PagedActionsResponse> {
    return this.http.get<PagedActionsResponse>(
      `${environment.apiUrl}/teams/${teamId}/actions/paged`,
      { params: { pageNumber, pageSize } },
    );
  }

  getCurrentActions(teamId: string): Observable<EngineeringActionsResponse> {
    return this.http.get<EngineeringActionsResponse>(
      `${environment.apiUrl}/teams/${teamId}/actions`,
    );
  }

  updateAction(
    teamId: string,
    actionId: string,
    request: UpdateEngineeringActionRequest,
  ): Observable<EngineeringAction> {
    return this.http.patch<EngineeringAction>(
      `${environment.apiUrl}/teams/${teamId}/actions/${actionId}`,
      request,
    );
  }
}
