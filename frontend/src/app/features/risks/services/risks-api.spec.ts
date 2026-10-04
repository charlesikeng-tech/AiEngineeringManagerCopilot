import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { RisksApi } from './risks-api';

describe('RisksApi page contract', () => {
  let api: RisksApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(RisksApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('preserves the legacy current-risks endpoint for dashboard consumers', () => {
    api.getCurrentRisks('team-a').subscribe();
    http.expectOne(`${environment.apiUrl}/teams/team-a/risks`).flush({ risks: [] });
  });

  it('requests a bounded page without deriving the full summary from its items', () => {
    const response = {
      teamId: 'team-a', reportId: 'report-1', periodStart: '2026-09-01', periodEnd: '2026-09-30',
      page: { items: [], totalCount: 34, pageNumber: 2, pageSize: 20 },
      summary: { criticalCount: 4, highCount: 10, mediumCount: 10, lowCount: 10 },
    };
    api.getRisksPage('team-a', 2, 20).subscribe((page) => expect(page).toEqual(response));
    const request = http.expectOne((candidate) => candidate.url === `${environment.apiUrl}/teams/team-a/risks/paged`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    request.flush(response);
  });
});
