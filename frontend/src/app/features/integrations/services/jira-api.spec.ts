import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { JiraApi } from './jira-api';

describe('JiraApi', () => {
  const teamId = 'team-id';
  const url = `${environment.apiUrl}/teams/${teamId}/jira`;
  let api: JiraApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(JiraApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the selected team connection', () => {
    api.getConnection(teamId).subscribe();
    const request = http.expectOne(url);
    expect(request.request.method).toBe('GET');
    request.flush({ teamId, projectKey: 'EM' });
  });

  it('sends credentials only when creating the connection', () => {
    const body = {
      baseUrl: 'https://example.atlassian.net',
      email: 'manager@example.com',
      apiToken: 'fake-token',
      projectKey: 'EM',
    };
    api.createConnection(teamId, body).subscribe();
    const request = http.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ teamId, projectKey: 'EM' });
  });

  it.each(['test', 'sync'] as const)('posts to the selected team %s endpoint', (operation) => {
    if (operation === 'test') {
      api.testConnection(teamId).subscribe();
    } else {
      api.sync(teamId).subscribe();
    }
    const request = http.expectOne(`${url}/${operation}`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush(
      operation === 'test'
        ? { isValid: true, displayName: 'Manager', message: 'Valid' }
        : { created: 1, updated: 2, total: 3 },
    );
  });

  it('deletes only the selected team connection', () => {
    api.deleteConnection(teamId).subscribe();
    const request = http.expectOne(url);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
