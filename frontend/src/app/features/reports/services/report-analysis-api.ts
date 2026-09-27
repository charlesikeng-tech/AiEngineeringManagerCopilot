import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AIAnalysis } from '../../dashboard/models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class ReportAnalysisApi {
  private readonly http = inject(HttpClient);

  getAnalysis(teamId: string, reportId: string): Observable<AIAnalysis> {
    return this.http.get<AIAnalysis>(
      `${environment.apiUrl}/teams/${teamId}/reports/${reportId}/analysis`,
    );
  }
}
