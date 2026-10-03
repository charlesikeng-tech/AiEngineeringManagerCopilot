import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { Team } from './models/team';
import { PagedResult } from '@core/models/paged-result';
@Injectable({
  providedIn: 'root',
})
export class TeamApi {
  private readonly http = inject(HttpClient);

  getTeams(): Observable<readonly Team[]> {
    return this.http.get<readonly Team[]>(`${environment.apiUrl}/teams`);
  }

  getTeamsPage(pageNumber: number, pageSize: number, search = ''): Observable<PagedResult<Team>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResult<Team>>(`${environment.apiUrl}/teams/paged`, { params });
  }
}
