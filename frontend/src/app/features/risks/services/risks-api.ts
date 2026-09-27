import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { EngineeringRisksResponse } from '../../dashboard/models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class RisksApi {
  private readonly http = inject(HttpClient);

  getCurrentRisks(teamId: string): Observable<EngineeringRisksResponse> {
    return this.http.get<EngineeringRisksResponse>(`${environment.apiUrl}/teams/${teamId}/risks`);
  }
}
