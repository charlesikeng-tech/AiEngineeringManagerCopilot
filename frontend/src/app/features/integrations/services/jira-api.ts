import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  CreateJiraConnectionRequest,
  JiraConnection,
  JiraSyncResult,
  TestJiraConnectionResponse,
} from '../models/jira-connection';

@Injectable({ providedIn: 'root' })
export class JiraApi {
  private readonly http = inject(HttpClient);

  getConnection(teamId: string): Observable<JiraConnection> {
    return this.http.get<JiraConnection>(this.connectionUrl(teamId));
  }

  createConnection(
    teamId: string,
    request: CreateJiraConnectionRequest,
  ): Observable<JiraConnection> {
    return this.http.post<JiraConnection>(this.connectionUrl(teamId), request);
  }

  testConnection(teamId: string): Observable<TestJiraConnectionResponse> {
    return this.http.post<TestJiraConnectionResponse>(`${this.connectionUrl(teamId)}/test`, {});
  }

  sync(teamId: string): Observable<JiraSyncResult> {
    return this.http.post<JiraSyncResult>(`${this.connectionUrl(teamId)}/sync`, {});
  }

  deleteConnection(teamId: string): Observable<void> {
    return this.http.delete<void>(this.connectionUrl(teamId));
  }

  private connectionUrl(teamId: string): string {
    return `${environment.apiUrl}/teams/${teamId}/jira`;
  }
}
