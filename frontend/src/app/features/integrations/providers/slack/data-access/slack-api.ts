import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { CreateSlackWebhookRequest } from '../models/create-slack-webhook-request';
import { SlackWebhookConnection } from '../models/slack-webhook-connection';
import { TestSlackWebhookResponse } from '../models/test-slack-webhook-response';

@Injectable({ providedIn: 'root' })
export class SlackApi {
  private readonly http = inject(HttpClient);

  getConnection(teamId: string): Observable<SlackWebhookConnection> {
    return this.http.get<SlackWebhookConnection>(this.connectionUrl(teamId));
  }

  createConnection(
    teamId: string,
    request: CreateSlackWebhookRequest,
  ): Observable<SlackWebhookConnection> {
    return this.http.post<SlackWebhookConnection>(this.connectionUrl(teamId), request);
  }

  deleteConnection(teamId: string): Observable<void> {
    return this.http.delete<void>(this.connectionUrl(teamId));
  }

  testConnection(teamId: string): Observable<TestSlackWebhookResponse> {
    return this.http.post<TestSlackWebhookResponse>(`${this.connectionUrl(teamId)}/test`, {});
  }

  private connectionUrl(teamId: string): string {
    return `${environment.apiUrl}/teams/${teamId}/slack`;
  }
}
