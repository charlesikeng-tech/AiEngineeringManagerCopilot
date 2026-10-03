import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TeamContext } from '@core/team/team-context';
import { environment } from '@environments/environment';
import { AppInitializer } from './app-initializer';

describe('Session application initialization', () => {
  let initializer: AppInitializer;
  let http: HttpTestingController;
  let teams: TeamContext;
  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    initializer = TestBed.inject(AppInitializer);
    http = TestBed.inject(HttpTestingController);
    teams = TestBed.inject(TeamContext);
    teams.selectTeam('previous-user-team');
  });
  afterEach(() => { http.verify(); localStorage.removeItem('selectedTeamId'); });

  it('completes unauthenticated startup and clears stale team context', () => {
    let completed = false;
    initializer.initialize().subscribe({ complete: () => completed = true });
    http.expectOne(`${environment.apiUrl}/auth/current`).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(completed).toBe(true);
    expect(initializer.initializationError()).toBeNull();
    expect(teams.selectedTeamId()).toBeNull();
  });

  it('loads only accessible teams without generating dated reports', () => {
    initializer.initialize().subscribe();
    http.expectOne(`${environment.apiUrl}/auth/current`).flush({ id: 'admin' });
    http.expectOne(`${environment.apiUrl}/teams`).flush([{ id: 'accessible-team' }]);
    expect(teams.selectedTeamId()).toBe('accessible-team');
  });

  it('allows a newly installed administrator to have no teams', () => {
    initializer.initializeTeams().subscribe();
    http.expectOne(`${environment.apiUrl}/teams`).flush([]);
    expect(teams.selectedTeamId()).toBeNull();
  });

  it('does not prevent the public login page loading when the API fails', () => {
    let completed = false;
    initializer.initialize().subscribe({ complete: () => completed = true });
    http.expectOne(`${environment.apiUrl}/auth/current`).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(completed).toBe(true);
    expect(initializer.initializationError()).toContain('Impossible');
  });
});
