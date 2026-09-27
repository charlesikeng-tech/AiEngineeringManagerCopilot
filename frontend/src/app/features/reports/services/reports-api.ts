import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { EngineeringReport } from '../../dashboard/models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class ReportsApi {
  private readonly http = inject(HttpClient);

  getReports(teamId: string): Observable<readonly EngineeringReport[]> {
    return this.http.get<readonly EngineeringReport[]>(
      `${environment.apiUrl}/teams/${teamId}/reports`,
    );
  }

  getReport(teamId: string, reportId: string): Observable<EngineeringReport> {
    return this.http.get<EngineeringReport>(
      `${environment.apiUrl}/teams/${teamId}/reports/${reportId}`,
    );
  }
}
