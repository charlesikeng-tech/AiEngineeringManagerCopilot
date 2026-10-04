import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { EngineeringRisksResponse } from '@features/dashboard/models/engineering-dashboard-response';
import { PagedRisksResponse } from '../models/paged-risks-response';

@Injectable({
  providedIn: 'root',
})
export class RisksApi {
  private readonly http = inject(HttpClient);

  getRisksPage(
    teamId: string,
    pageNumber: number,
    pageSize: number,
  ): Observable<PagedRisksResponse> {
    return this.http.get<PagedRisksResponse>(`${environment.apiUrl}/teams/${teamId}/risks/paged`, {
      params: { pageNumber, pageSize },
    });
  }

  getCurrentRisks(teamId: string): Observable<EngineeringRisksResponse> {
    return this.http.get<EngineeringRisksResponse>(`${environment.apiUrl}/teams/${teamId}/risks`);
  }
}
