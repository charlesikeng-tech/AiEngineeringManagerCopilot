import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { ReportsApi } from './reports-api';

describe('ReportsApi page contract', () => {
  let api: ReportsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ReportsApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('preserves the legacy all-reports endpoint', () => {
    api.getReports('team-a').subscribe();
    http.expectOne(`${environment.apiUrl}/teams/team-a/reports`).flush([]);
  });

  it('requests one bounded page and passes metadata and server deltas through', () => {
    const response = { items: [{ id: 'report-11', scoreDelta: -7, coverageDelta: 3 }], totalCount: 24, pageNumber: 2, pageSize: 10 };
    api.getReportsPage('team-a', 2, 10).subscribe((page) => expect(page).toEqual(response));
    const request = http.expectOne((candidate) => candidate.url === `${environment.apiUrl}/teams/team-a/reports/paged`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush(response);
  });
});
