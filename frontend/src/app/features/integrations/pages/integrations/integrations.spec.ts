import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import {
  GithubOutline,
  SlackOutline,
  SyncOutline,
  TeamOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { PagedResult } from '@core/models/paged-result';
import { Team } from '@domains/teams/models/team';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { TeamContext } from '@core/team/team-context';
import { GitHubConnection } from '@features/integrations/providers/github/models/github-connection';
import { GitHubApi } from '@features/integrations/providers/github/data-access/github-api';
import { MicrosoftTeamsApi } from '@features/integrations/providers/microsoft-teams/data-access/microsoft-teams-api';
import { SlackApi } from '@features/integrations/providers/slack/data-access/slack-api';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, Subject, of, throwError } from 'rxjs';
import teamEn from '../../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../../public/i18n/team/fr.json';
import { JiraApi } from '@features/integrations/providers/jira/data-access/jira-api';
import { IntegrationsPage } from './integrations';

describe('all-team integrations overview', () => {
  const teams: readonly Team[] = ['Platform', 'Backend'].map((name, index) => ({
    id: `team-${index}`,
    ownerUserId: 'owner',
    name,
    description: null,
    createdAt: '2026-10-01T00:00:00Z',
  }));
  const github = (teamId: string): GitHubConnection => ({
    id: `github-${teamId}`,
    teamId,
    owner: 'example',
    ownerType: 'Organization',
    createdAt: '2026-10-01T00:00:00Z',
    lastSyncAt: teamId === teams[0].id ? '2026-10-03T12:00:00Z' : null,
  });
  const missing = () => new HttpErrorResponse({ status: 404 });
  const getTeams = vi.fn<() => Observable<readonly Team[]>>();
  const getTeamsPage =
    vi.fn<
      (pageNumber: number, pageSize: number, search?: string) => Observable<PagedResult<Team>>
    >();
  const getGithub = vi.fn<(teamId: string) => Observable<GitHubConnection>>();
  const getJira = vi.fn();
  const getSlack = vi.fn();
  const getMicrosoftTeams = vi.fn();

  const pageOf = (
    items: readonly Team[],
    pageNumber = 1,
    pageSize = 10,
    totalCount = items.length,
  ): PagedResult<Team> => ({ items, totalCount, pageNumber, pageSize });

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    getTeams.mockReset().mockReturnValue(of(teams));
    getTeamsPage.mockReset().mockImplementation((pageNumber, pageSize, search = '') => {
      const matches = teams.filter((team) =>
        team.name.toLowerCase().includes(search.toLowerCase()),
      );
      return of(
        pageOf(
          matches.slice((pageNumber - 1) * pageSize, pageNumber * pageSize),
          pageNumber,
          pageSize,
          matches.length,
        ),
      );
    });
    getGithub.mockReset().mockImplementation((teamId) => of(github(teamId)));
    getJira.mockReset().mockReturnValue(throwError(missing));
    getSlack.mockReset().mockReturnValue(throwError(missing));
    getMicrosoftTeams
      .mockReset()
      .mockImplementation((teamId: string) =>
        teamId === teams[0].id
          ? of({ teamId, createdAt: '2026-10-01T00:00:00Z' })
          : throwError(missing),
      );
    TestBed.configureTestingModule({
      imports: [IntegrationsPage],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: teamEn, fr: teamFr }),
        provideNzIcons([GithubOutline, SlackOutline, SyncOutline, TeamOutline]),
        { provide: TeamApi, useValue: { getTeams, getTeamsPage } },
        { provide: GitHubApi, useValue: { getConnection: getGithub } },
        { provide: JiraApi, useValue: { getConnection: getJira } },
        { provide: SlackApi, useValue: { getConnection: getSlack } },
        { provide: MicrosoftTeamsApi, useValue: { getConnection: getMicrosoftTeams } },
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  async function render() {
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('requests only the default page without requiring or changing an active team', async () => {
    const fixture = await render();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBeNull();
    expect(fixture.componentInstance.configuring()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('Platform');
    expect(fixture.nativeElement.textContent).toContain('Backend');
    expect(getTeamsPage).toHaveBeenCalledWith(1, 10, '');
    expect(getTeams).not.toHaveBeenCalled();
    for (const team of teams) {
      expect(getGithub).toHaveBeenCalledWith(team.id);
      expect(getJira).toHaveBeenCalledWith(team.id);
    }
  });

  it('distinguishes recorded sync dates, never synchronized, and notification configuration', async () => {
    const fixture = await render();
    const page = fixture.componentInstance;
    expect(page.rows()[0].github.lastSyncAt).toBe('2026-10-03T12:00:00Z');
    expect(page.rows()[1].github.lastSyncAt).toBeNull();
    expect(page.rows()[1].jira.status).toBe('notConnected');
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows[0].querySelector('time').getAttribute('datetime')).toBe('2026-10-03T12:00:00Z');
    expect(rows[1].textContent).toContain('Never');
    expect(rows[0].textContent).toContain('Configured');
    expect(fixture.nativeElement.textContent).toContain('do not confirm');
  });

  it('groups providers and keeps compact statuses and pagination in one panel', async () => {
    const fixture = await render();
    const panel = fixture.nativeElement.querySelector('.overview-panel');
    expect(panel.querySelector('.overview-toolbar')).not.toBeNull();
    expect(panel.querySelector('.overview-pagination nz-pagination')).not.toBeNull();
    const groups = panel.querySelectorAll('.table-groups th[colspan="2"]');
    expect(groups).toHaveLength(2);
    expect(groups[0].textContent).toContain('Data sources');
    expect(groups[1].textContent).toContain('Notifications');
    expect(panel.querySelector('caption').classList.contains('table-caption')).toBe(true);
    expect(panel.querySelector('.connection-status--connected .status-dot')).not.toBeNull();
    expect(panel.querySelector('time').title).toContain('Last recorded synchronization');
    const note = fixture.nativeElement.querySelector('details.overview-note');
    expect(note.hasAttribute('open')).toBe(false);
    expect(note.querySelector('summary').textContent).toContain('About synchronization statuses');
  });

  it('searches on the server without switching the active team', async () => {
    TestBed.inject(TeamContext).selectTeam(teams[0].id);
    const fixture = await render();
    fixture.componentInstance.setSearch('Backend');
    await new Promise((resolve) => setTimeout(resolve, 320));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Backend');
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(teams[0].id);
    expect(getTeamsPage).toHaveBeenLastCalledWith(1, 10, 'Backend');
    expect(getGithub).toHaveBeenCalledTimes(3);
  });

  it('keeps GitHub and Jira synchronization dates separate for each team', async () => {
    getJira.mockImplementation((teamId: string) =>
      of({
        id: `jira-${teamId}`,
        teamId,
        baseUrl: 'https://example.atlassian.net',
        email: 'manager@example.com',
        projectKey: 'EM',
        createdAt: '2026-10-01T00:00:00Z',
        lastSyncAt: '2026-10-02T10:00:00Z',
      }),
    );
    const fixture = await render();
    const dates = fixture.nativeElement.querySelector('tbody tr').querySelectorAll('time');
    expect(dates[0].getAttribute('datetime')).toBe('2026-10-03T12:00:00Z');
    expect(dates[1].getAttribute('datetime')).toBe('2026-10-02T10:00:00Z');
    expect(fixture.componentInstance.rows()[1].github.lastSyncAt).toBeNull();
    expect(fixture.componentInstance.rows()[1].jira.lastSyncAt).toBe('2026-10-02T10:00:00Z');
  });

  it('loads at most four teams concurrently without overwhelming provider APIs', async () => {
    const manyTeams = Array.from({ length: 10 }, (_, index) => ({
      ...teams[0],
      id: `team-${index}`,
    }));
    const pending = new Map(manyTeams.map((team) => [team.id, new Subject<GitHubConnection>()]));
    getTeamsPage.mockReturnValue(of(pageOf(manyTeams)));
    getGithub.mockImplementation((teamId) => {
      const request = pending.get(teamId);
      if (!request) {
        throw new Error(`Unexpected team ${teamId}`);
      }
      return request;
    });
    await render();
    expect(getGithub).toHaveBeenCalledTimes(4);
    expect(getJira).toHaveBeenCalledTimes(4);
    const first = pending.get(manyTeams[0].id)!;
    first.next(github(manyTeams[0].id));
    first.complete();
    expect(getGithub).toHaveBeenCalledTimes(5);
    expect(getGithub).toHaveBeenLastCalledWith(manyTeams[4].id);
  });

  it('opens settings for the chosen team and refreshes the overview on return', async () => {
    const fixture = await render();
    const page = fixture.componentInstance;
    page.configure(teams[1].id);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(teams[1].id);
    expect(fixture.nativeElement.querySelector('app-integration-settings')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();

    getGithub.mockImplementation((teamId) =>
      of({ ...github(teamId), lastSyncAt: '2026-10-03T14:00:00Z' }),
    );
    page.showOverview();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('app-integration-settings')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(page.rows()[1].github.lastSyncAt).toBe('2026-10-03T14:00:00Z');
  });

  it('shows provider load errors without hiding other team connections', async () => {
    getGithub.mockImplementation((teamId) =>
      teamId === teams[0].id
        ? throwError(() => new HttpErrorResponse({ status: 500 }))
        : of(github(teamId)),
    );
    const fixture = await render();
    expect(fixture.componentInstance.loadError()).toBe(false);
    expect(fixture.componentInstance.rows()[0].github.status).toBe('error');
    expect(fixture.componentInstance.rows()[0].microsoftTeams.status).toBe('connected');
    expect(fixture.componentInstance.rows()[1].github.status).toBe('connected');
    expect(fixture.nativeElement.textContent).toContain('Unable to load');
  });

  it('does not mistake authorization errors for missing connections', async () => {
    getJira.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 403 })));
    const fixture = await render();
    expect(fixture.componentInstance.rows().every((row) => row.jira.status === 'error')).toBe(true);
  });

  it('shows a team-list failure and allows retry', async () => {
    getTeamsPage.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 503 })));
    const fixture = await render();
    expect(fixture.componentInstance.loadError()).toBe(true);
    expect(getGithub).not.toHaveBeenCalled();
    getTeamsPage.mockReturnValue(of(pageOf(teams)));
    fixture.componentInstance.refresh();
    fixture.detectChanges();
    expect(fixture.componentInstance.loadError()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(2);
  });

  it('renders a no-team state without fetching integrations', async () => {
    getTeamsPage.mockReturnValue(of(pageOf([])));
    const fixture = await render();
    expect(fixture.nativeElement.textContent).toContain('No teams yet');
    expect(getGithub).not.toHaveBeenCalled();
    expect(fixture.componentInstance.refreshing()).toBe(false);
  });

  it('cancels stale refreshes and keeps the newest states', async () => {
    const stale = new Subject<GitHubConnection>();
    getGithub.mockReturnValue(stale);
    const fixture = await render();
    expect(fixture.componentInstance.rows()[0].github.status).toBe('loading');
    getGithub.mockImplementation((teamId) => of(github(teamId)));
    fixture.componentInstance.refresh();
    stale.next({ ...github(teams[0].id), owner: 'outdated', lastSyncAt: '2000-01-01T00:00:00Z' });
    expect(stale.observed).toBe(false);
    expect(fixture.componentInstance.rows()[0].github.lastSyncAt).toBe('2026-10-03T12:00:00Z');
    expect(fixture.componentInstance.refreshing()).toBe(false);
  });

  it('localizes the overview without resetting its filter', async () => {
    const fixture = await render();
    fixture.componentInstance.setSearch('Backend');
    await new Promise((resolve) => setTimeout(resolve, 320));
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Rechercher une équipe');
    expect(fixture.nativeElement.textContent).toContain('Jamais');
    expect(fixture.componentInstance.searchText()).toBe('Backend');
  });

  it('loads integrations only for the current page and lazily fetches the next page', async () => {
    const allTeams = Array.from({ length: 25 }, (_, index) => ({
      ...teams[0],
      id: `paged-team-${index}`,
      name: `Team ${index}`,
    }));
    getTeamsPage.mockImplementation((pageNumber, pageSize) =>
      of(
        pageOf(
          allTeams.slice((pageNumber - 1) * pageSize, pageNumber * pageSize),
          pageNumber,
          pageSize,
          allTeams.length,
        ),
      ),
    );
    const fixture = await render();
    const page = fixture.componentInstance;
    expect(page.totalCount()).toBe(25);
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(10);
    expect(getGithub).toHaveBeenCalledTimes(10);
    expect(getGithub).not.toHaveBeenCalledWith(allTeams[10].id);
    const secondPage = fixture.nativeElement.querySelector('.ant-pagination-item[title="2"]');
    secondPage.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getTeamsPage).toHaveBeenLastCalledWith(2, 10, '');
    expect(page.rows()[0].team.id).toBe(allTeams[10].id);
    expect(getGithub).toHaveBeenCalledTimes(20);
    expect(getGithub).not.toHaveBeenCalledWith(allTeams[20].id);
    expect(getTeams).not.toHaveBeenCalled();
  });

  it('resets to the first page on page-size and search changes', async () => {
    const allTeams = Array.from({ length: 25 }, (_, index) => ({
      ...teams[0],
      id: `paged-team-${index}`,
    }));
    getTeamsPage.mockImplementation((pageNumber, pageSize) =>
      of(
        pageOf(
          allTeams.slice((pageNumber - 1) * pageSize, pageNumber * pageSize),
          pageNumber,
          pageSize,
          25,
        ),
      ),
    );
    const fixture = await render();
    const page = fixture.componentInstance;
    page.changePage(3);
    fixture.detectChanges();
    fixture.debugElement
      .query(By.css('.app-table-footer nz-select'))
      .triggerEventHandler('ngModelChange', 20);
    fixture.detectChanges();
    expect(getTeamsPage).toHaveBeenLastCalledWith(1, 20, '');
    expect(page.rows()).toHaveLength(20);
    page.changePage(2);
    page.setSearch(' Platform ');
    await new Promise((resolve) => setTimeout(resolve, 320));
    expect(getTeamsPage).toHaveBeenLastCalledWith(1, 20, 'Platform');
    expect(page.pageNumber()).toBe(1);
  });

  it('preserves pagination and search after configuring a team', async () => {
    getTeamsPage.mockImplementation((pageNumber, pageSize) =>
      of(pageOf([teams[1]], pageNumber, pageSize, 25)),
    );
    const fixture = await render();
    const page = fixture.componentInstance;
    page.setSearch('Backend');
    await new Promise((resolve) => setTimeout(resolve, 320));
    page.changePage(2);
    page.configure(teams[1].id);
    page.showOverview();
    expect(getTeamsPage).toHaveBeenLastCalledWith(2, 10, 'Backend');
    expect(page.pageNumber()).toBe(2);
    expect(page.search()).toBe('Backend');
  });

  it('cancels in-flight team-page requests when a newer page is requested', async () => {
    const stale = new Subject<PagedResult<Team>>();
    getTeamsPage.mockReturnValueOnce(stale);
    const fixture = await render();
    expect(getGithub).not.toHaveBeenCalled();
    getTeamsPage.mockReturnValue(of(pageOf([teams[1]], 2, 10, 25)));
    fixture.componentInstance.changePage(2);
    stale.next(pageOf([teams[0]], 1, 10, 25));
    expect(stale.observed).toBe(false);
    expect(fixture.componentInstance.rows()[0].team.id).toBe(teams[1].id);
    expect(getGithub).not.toHaveBeenCalledWith(teams[0].id);
    expect(fixture.componentInstance.loadingTeams()).toBe(false);
  });

  it('returns to a valid page if the last page disappeared after deleting teams', async () => {
    const fixture = await render();
    getTeamsPage
      .mockReturnValueOnce(of(pageOf([], 3, 10, 15)))
      .mockReturnValueOnce(of(pageOf([teams[1]], 2, 10, 15)));
    fixture.componentInstance.changePage(3);
    expect(getTeamsPage).toHaveBeenLastCalledWith(2, 10, '');
    expect(fixture.componentInstance.pageNumber()).toBe(2);
    expect(fixture.componentInstance.rows()[0].team.id).toBe(teams[1].id);
  });

  it('distinguishes an empty search result from having no teams', async () => {
    const fixture = await render();
    fixture.componentInstance.setSearch('unmatched');
    await new Promise((resolve) => setTimeout(resolve, 320));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No teams match this search');
    expect(getTeamsPage).toHaveBeenLastCalledWith(1, 10, 'unmatched');
  });
});
