import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { TeamApi } from './team-api';

describe('feature TeamApi pagination', () => {
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

  it('preserves legacy team and member endpoints for other consumers', () => {
    api.getTeams().subscribe();
    http.expectOne(`${environment.apiUrl}/teams/`).flush([]);
    api.getMembers('team-id').subscribe();
    http.expectOne(`${environment.apiUrl}/teams/team-id/members/`).flush([]);
  });

  it('requests a bounded teams page with encoded search and retains metadata', () => {
    api.getTeamsPage(2, 20, ' Platform & API ').subscribe((page) => {
      expect(page).toEqual({ items: [], totalCount: 35, pageNumber: 2, pageSize: 20 });
    });
    const request = http.expectOne((candidate) => candidate.url === `${environment.apiUrl}/teams/paged`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.get('search')).toBe('Platform & API');
    request.flush({ items: [], totalCount: 35, pageNumber: 2, pageSize: 20 });
  });

  it('omits blank team searches', () => {
    api.getTeamsPage(1, 10, ' ').subscribe();
    const request = http.expectOne((candidate) => candidate.url === `${environment.apiUrl}/teams/paged`);
    expect(request.request.params.has('search')).toBe(false);
    request.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10 });
  });

  it('requests only the selected team member page and retains full total', () => {
    api.getMembersPage('team-id', 3, 10).subscribe((page) => {
      expect(page.totalCount).toBe(24);
      expect(page.items).toEqual([]);
    });
    const request = http.expectOne((candidate) => candidate.url === `${environment.apiUrl}/teams/team-id/members/paged`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('3');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush({ items: [], totalCount: 24, pageNumber: 3, pageSize: 10 });
  });
});
