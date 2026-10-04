import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { PagedResult } from '@core/models/paged-result';
import { CreateTeamRequest, Team, UpdateTeamRequest } from '../models/team';
import {
  CreateTeamMemberRequest,
  TeamMember,
  UpdateTeamMemberRequest,
} from '../models/team-member';

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

  getMembers(teamId: string): Observable<readonly TeamMember[]> {
    return this.http.get<readonly TeamMember[]>(`${environment.apiUrl}/teams/${teamId}/members/`);
  }

  getMembersPage(
    teamId: string,
    pageNumber: number,
    pageSize: number,
  ): Observable<PagedResult<TeamMember>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<TeamMember>>(
      `${environment.apiUrl}/teams/${teamId}/members/paged`,
      { params },
    );
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
