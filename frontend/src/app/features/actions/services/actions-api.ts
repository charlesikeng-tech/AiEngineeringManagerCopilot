import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  EngineeringAction,
  EngineeringActionsResponse,
  UpdateEngineeringActionRequest,
} from '../../dashboard/models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class ActionsApi {
  private readonly http = inject(HttpClient);

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
