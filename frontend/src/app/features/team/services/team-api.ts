import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  CreateTeamMemberRequest,
  CreateTeamRequest,
  Team,
  TeamMember,
  UpdateTeamMemberRequest,
  UpdateTeamRequest,
} from '../models/team.model';

@Injectable({
  providedIn: 'root',
})
export class TeamApi {
  private readonly http = inject(HttpClient);

  getTeams(): Observable<Team[]> {
    return this.http.get<Team[]>(`${environment.apiUrl}/teams/`);
  }

  getTeam(teamId: string): Observable<Team> {
    return this.http.get<Team>(`${environment.apiUrl}/teams/${teamId}`);
  }

  createTeam(request: CreateTeamRequest): Observable<Team> {
    return this.http.post<Team>(`${environment.apiUrl}/teams/`, request);
  }

  updateTeam(teamId: string, request: UpdateTeamRequest): Observable<Team> {
    return this.http.put<Team>(`${environment.apiUrl}/teams/${teamId}`, request);
  }

  deleteTeam(teamId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/teams/${teamId}`);
  }

  getMembers(teamId: string): Observable<TeamMember[]> {
    return this.http.get<TeamMember[]>(`${environment.apiUrl}/teams/${teamId}/members/`);
  }

  getMember(teamId: string, memberId: string): Observable<TeamMember> {
    return this.http.get<TeamMember>(`${environment.apiUrl}/teams/${teamId}/members/${memberId}`);
  }

  createMember(teamId: string, request: CreateTeamMemberRequest): Observable<TeamMember> {
    return this.http.post<TeamMember>(`${environment.apiUrl}/teams/${teamId}/members/`, request);
  }

  updateMember(
    teamId: string,
    memberId: string,
    request: UpdateTeamMemberRequest,
  ): Observable<TeamMember> {
    return this.http.put<TeamMember>(
      `${environment.apiUrl}/teams/${teamId}/members/${memberId}`,
      request,
    );
  }

  deleteMember(teamId: string, memberId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/teams/${teamId}/members/${memberId}`);
  }
}
