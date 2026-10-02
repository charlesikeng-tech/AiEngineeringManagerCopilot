import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { CreateGitHubConnectionRequest } from '../models/create-github-connection-request';
import { GitHubConnection } from '../models/github-connection';
import { GitHubConnectionTestResponse } from '../models/github-connection-test-response';
import { GitHubSyncResponse } from '../models/github-sync-response';

@Injectable({
  providedIn: 'root',
})
export class GitHubApi {
  private readonly http = inject(HttpClient);

  getConnection(teamId: string): Observable<GitHubConnection> {
    return this.http.get<GitHubConnection>(this.connectionUrl(teamId));
  }

  createConnection(
    teamId: string,
    request: CreateGitHubConnectionRequest,
  ): Observable<GitHubConnection> {
    return this.http.post<GitHubConnection>(this.connectionUrl(teamId), request);
  }

  deleteConnection(teamId: string): Observable<void> {
    return this.http.delete<void>(this.connectionUrl(teamId));
  }

  testConnection(teamId: string): Observable<GitHubConnectionTestResponse> {
    return this.http.post<GitHubConnectionTestResponse>(`${this.connectionUrl(teamId)}/test`, {});
  }

  sync(teamId: string): Observable<GitHubSyncResponse> {
    return this.http.post<GitHubSyncResponse>(`${this.connectionUrl(teamId)}/sync`, {});
  }

  private connectionUrl(teamId: string): string {
    return `${environment.apiUrl}/teams/${teamId}/github`;
  }
}
