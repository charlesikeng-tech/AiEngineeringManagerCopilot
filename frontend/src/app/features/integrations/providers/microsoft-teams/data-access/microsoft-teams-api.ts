import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '@environments/environment';
import { CreateMicrosoftTeamsWebhookRequest } from '../models/create-microsoft-teams-webhook-request';
import { MicrosoftTeamsWebhookConnection } from '../models/microsoft-teams-webhook-connection';
import { TestMicrosoftTeamsWebhookResponse } from '../models/test-microsoft-teams-webhook-response';

@Injectable({ providedIn: 'root' })
export class MicrosoftTeamsApi {
  private readonly http = inject(HttpClient);

  getConnection(teamId: string): Observable<MicrosoftTeamsWebhookConnection> {
    return this.http.get<MicrosoftTeamsWebhookConnection>(this.connectionUrl(teamId));
  }

  createConnection(
    teamId: string,
    request: CreateMicrosoftTeamsWebhookRequest,
  ): Observable<MicrosoftTeamsWebhookConnection> {
    return this.http.post<MicrosoftTeamsWebhookConnection>(this.connectionUrl(teamId), request);
  }

  deleteConnection(teamId: string): Observable<void> {
    return this.http.delete<void>(this.connectionUrl(teamId));
  }

  testConnection(teamId: string): Observable<TestMicrosoftTeamsWebhookResponse> {
    return this.http.post<TestMicrosoftTeamsWebhookResponse>(`${this.connectionUrl(teamId)}/test`, {});
  }

  private connectionUrl(teamId: string): string {
    return `${environment.apiUrl}/teams/${teamId}/microsoft-teams`;
  }
}
