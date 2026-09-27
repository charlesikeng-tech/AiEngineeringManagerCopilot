import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Team } from './models/team';
@Injectable({
  providedIn: 'root',
})
export class TeamApi {
  private readonly http = inject(HttpClient);

  getTeams(): Observable<readonly Team[]> {
    return this.http.get<readonly Team[]>(`${environment.apiUrl}/teams`);
  }
}
