import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import type { AIAnalysis } from '@domains/engineering/models/ai-analysis';

@Injectable({
  providedIn: 'root',
})
export class ReportAnalysisApi {
  private readonly http = inject(HttpClient);

  getAnalysis(teamId: string, reportId: string): Observable<AIAnalysis> {
    return this.http.get<AIAnalysis>(`${this.reportUrl(teamId, reportId)}/analysis`);
  }

  generateAnalysis(teamId: string, reportId: string): Observable<AIAnalysis> {
    return this.http.post<AIAnalysis>(`${this.reportUrl(teamId, reportId)}/analyze`, null);
  }

  private reportUrl(teamId: string, reportId: string): string {
    return `${environment.apiUrl}/teams/${teamId}/reports/${reportId}`;
  }
}
