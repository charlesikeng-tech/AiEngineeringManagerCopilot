import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';

import { MicrosoftTeamsApi } from './microsoft-teams-api';

describe('MicrosoftTeamsApi', () => {
  const teamId = 'team-id';
  const connectionUrl = `${environment.apiUrl}/teams/${teamId}/microsoft-teams`;
  let api: MicrosoftTeamsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(MicrosoftTeamsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets the connection without webhook credentials', () => {
    const connection = { teamId, createdAt: '2026-09-01T00:00:00Z' };
    api.getConnection(teamId).subscribe((result) => expect(result).toEqual(connection));
    const request = http.expectOne(connectionUrl);
    expect(request.request.method).toBe('GET');
    request.flush(connection);
  });

  it('posts a webhook to create the connection', () => {
    const body = { webhookUrl: 'https://outlook.office.com/webhook/id/test' };
    api.createConnection(teamId, body).subscribe();
    const request = http.expectOne(connectionUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ teamId, createdAt: '2026-09-01T00:00:00Z' });
  });

  it('posts an empty body to test the connection', () => {
    const result = { success: true, message: 'Sent.' };
    api.testConnection(teamId).subscribe((response) => expect(response).toEqual(result));
    const request = http.expectOne(`${connectionUrl}/test`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush(result);
  });

  it('deletes the connection', () => {
    api.deleteConnection(teamId).subscribe();
    const request = http.expectOne(connectionUrl);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
