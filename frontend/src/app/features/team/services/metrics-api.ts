import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { EngineeringMetric } from '../models/engineering-metric';

@Injectable({
  providedIn: 'root',
})
export class MetricsApi {
  private readonly http = inject(HttpClient);

  calculate(
    teamId: string,
    periodStart: string,
    periodEnd: string,
  ): Observable<EngineeringMetric[]> {
    const params = new HttpParams().set('periodStart', periodStart).set('periodEnd', periodEnd);

    return this.http.post<EngineeringMetric[]>(
      `${environment.apiUrl}/teams/${teamId}/metrics/calculate`,
      null,
      { params },
    );
  }
}
