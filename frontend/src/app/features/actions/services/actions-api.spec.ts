import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { ActionsApi } from './actions-api';

describe('ActionsApi page contract', () => {
  let api: ActionsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ActionsApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('preserves the legacy current-actions endpoint for dashboard consumers', () => {
    api.getCurrentActions('team-a').subscribe();
    http.expectOne(`${environment.apiUrl}/teams/team-a/actions`).flush({ actions: [] });
  });

  it('requests one page and retains the global status, overdue and UTC-date summary', () => {
    const response = {
      teamId: 'team-a', reportId: 'report-1', periodStart: '2026-09-01', periodEnd: '2026-09-30',
      page: { items: [], totalCount: 64, pageNumber: 2, pageSize: 50 },
      summary: { todoCount: 30, inProgressCount: 20, doneCount: 10, cancelledCount: 4, overdueCount: 12, asOfDate: '2026-09-30' },
    };
    api.getActionsPage('team-a', 2, 50).subscribe((page) => expect(page).toEqual(response));
    const request = http.expectOne((candidate) => candidate.url === `${environment.apiUrl}/teams/team-a/actions/paged`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('50');
    request.flush(response);
  });

  it('keeps action edits on the existing PATCH route', () => {
    api.updateAction('team-a', 'action-1', { status: 'Done' }).subscribe();
    const request = http.expectOne(`${environment.apiUrl}/teams/team-a/actions/action-1`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ status: 'Done' });
    request.flush({ id: 'action-1', status: 'Done' });
  });
});
