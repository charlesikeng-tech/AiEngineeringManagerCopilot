import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { TeamApi } from './team-api';

describe('TeamApi pagination', () => {
  let api: TeamApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(TeamApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('keeps the existing all-team endpoint unchanged', () => {
    api.getTeams().subscribe();
    http.expectOne(`${environment.apiUrl}/teams`).flush([]);
  });

  it('requests one page with a safely encoded server-side name search', () => {
    api
      .getTeamsPage(2, 20, ' Platform & API ')
      .subscribe((page) => expect(page.totalCount).toBe(25));
    const request = http.expectOne(
      (candidate) => candidate.url === `${environment.apiUrl}/teams/paged`,
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.get('search')).toBe('Platform & API');
    request.flush({ items: [], totalCount: 25, pageNumber: 2, pageSize: 20 });
  });

  it('omits empty searches', () => {
    api.getTeamsPage(1, 10).subscribe();
    const request = http.expectOne(
      (candidate) => candidate.url === `${environment.apiUrl}/teams/paged`,
    );
    expect(request.request.params.has('search')).toBe(false);
    request.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10 });
  });
});
