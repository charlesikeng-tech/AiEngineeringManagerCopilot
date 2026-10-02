import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { EngineeringDashboardResponse } from '../models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class DashboardApi {
  private readonly http = inject(HttpClient);

  getDashboard(teamId: string): Observable<EngineeringDashboardResponse> {
    return this.http.get<EngineeringDashboardResponse>(
      `${environment.apiUrl}/teams/${teamId}/dashboard`,
    );
  }
}
