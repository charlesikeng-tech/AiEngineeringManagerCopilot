import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, Observable, of, switchMap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AIAnalysis,
  EngineeringReport,
} from '../../features/dashboard/models/engineering-dashboard-response';

@Injectable({
  providedIn: 'root',
})
export class DevelopmentBootstrap {
  private readonly http = inject(HttpClient);

  ensureReport(
    teamId: string,
    periodStart: string,
    periodEnd: string,
  ): Observable<EngineeringReport> {
    return this.getReports(teamId).pipe(
      switchMap((reports) => {
        const existingReport = reports.find(
          (report) => report.periodStart === periodStart && report.periodEnd === periodEnd,
        );

        if (existingReport) {
          return of(existingReport);
        }

        return this.generateReport(teamId, periodStart, periodEnd);
      }),
    );
  }

  private getReports(teamId: string): Observable<readonly EngineeringReport[]> {
    return this.http.get<readonly EngineeringReport[]>(
      `${environment.apiUrl}/teams/${teamId}/reports`,
    );
  }

  private generateReport(
    teamId: string,
    periodStart: string,
    periodEnd: string,
  ): Observable<EngineeringReport> {
    const params = new HttpParams().set('periodStart', periodStart).set('periodEnd', periodEnd);

    return this.http.post<EngineeringReport>(
      `${environment.apiUrl}/teams/${teamId}/reports`,
      null,
      { params },
    );
  }

  ensureAnalysis(teamId: string, reportId: string): Observable<AIAnalysis> {
    return this.getAnalysis(teamId, reportId).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return this.generateAnalysis(teamId, reportId);
        }

        return throwError(() => error);
      }),
    );
  }

  private getAnalysis(teamId: string, reportId: string): Observable<AIAnalysis> {
    return this.http.get<AIAnalysis>(
      `${environment.apiUrl}/teams/${teamId}/reports/${reportId}/analysis`,
    );
  }

  private generateAnalysis(teamId: string, reportId: string): Observable<AIAnalysis> {
    return this.http.post<AIAnalysis>(
      `${environment.apiUrl}/teams/${teamId}/reports/${reportId}/analyze`,
      null,
    );
  }
}
