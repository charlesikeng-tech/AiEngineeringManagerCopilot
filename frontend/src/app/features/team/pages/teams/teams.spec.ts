import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { PlusOutline } from '@ant-design/icons-angular/icons';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, throwError } from 'rxjs';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService } from '@core/i18n/i18n.service';
import { PagedResult } from '@core/models/paged-result';
import { TeamContext } from '@core/team/team-context';
import { Team } from '@domains/teams/models/team';
import { TeamApi } from '@domains/teams/data-access/team-api';
import teamsEn from '../../../../../../public/i18n/team/en.json';
import teamsFr from '../../../../../../public/i18n/team/fr.json';
import { TeamsPage } from './teams';

describe('TeamsPage lazy catalog', () => {
  const team: Team = { id: 'team-id', ownerUserId: 'owner-id', name: 'Platform', description: 'Platform engineering', createdAt: '2026-09-01' };
  const getTeams = vi.fn();
  const getTeamsPage = vi.fn<(...args: [number, number]) => Observable<PagedResult<Team>>>();
  const createTeam = vi.fn(() => of({ ...team, id: 'new-team' }));
  const navigate = vi.fn(() => Promise.resolve(true));

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    vi.clearAllMocks();
    getTeamsPage.mockReset().mockReturnValue(of({ items: [team], totalCount: 35, pageNumber: 1, pageSize: 10 }));
    TestBed.configureTestingModule({
      imports: [TeamsPage],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: teamsEn, fr: teamsFr }),
        provideNzIcons([PlusOutline]),
        { provide: TeamApi, useValue: { getTeams, getTeamsPage, createTeam } },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigate').mockImplementation(navigate);
  });

  afterEach(() => localStorage.removeItem('selectedTeamId'));

  it('loads only the default page, uses total metadata and preserves accessible cards', async () => {
    const fixture = TestBed.createComponent(TeamsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getTeamsPage).toHaveBeenCalledExactlyOnceWith(1, 10);
    expect(getTeams).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.page-summary__value').textContent).toContain('35');
    const cards = fixture.nativeElement.querySelectorAll('.team-card');
    expect(cards).toHaveLength(1);
    expect(cards[0].getAttribute('role')).toBe('button');
    expect(cards[0].getAttribute('tabindex')).toBe('0');
    cards[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    expect(navigate).toHaveBeenCalledWith(['/team', team.id]);
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(team.id);
  });

  it('requests the next page and resets page when changing size to twenty', () => {
    const page = TestBed.createComponent(TeamsPage).componentInstance;
    page.changePage(2);
    expect(getTeamsPage).toHaveBeenLastCalledWith(2, 10);
    page.changePageSize(20);
    expect(getTeamsPage).toHaveBeenLastCalledWith(1, 20);
    page.changePageSize(20);
    expect(getTeamsPage).toHaveBeenCalledTimes(3);
    expect(getTeams).not.toHaveBeenCalled();
  });

  it('cancels an obsolete page load', () => {
    const cancelled = vi.fn();
    getTeamsPage.mockReturnValueOnce(new Observable(() => cancelled));
    const page = TestBed.createComponent(TeamsPage).componentInstance;
    page.changePage(2);
    expect(cancelled).toHaveBeenCalledOnce();
  });

  it('clamps and refetches when the requested page is no longer available', () => {
    const page = TestBed.createComponent(TeamsPage).componentInstance;
    getTeamsPage.mockReturnValueOnce(of({ items: [], totalCount: 10, pageNumber: 4, pageSize: 10 }));
    page.changePage(4);
    expect(getTeamsPage.mock.calls.slice(-2)).toEqual([[4, 10], [1, 10]]);
    expect(page.pagination.pageNumber()).toBe(1);
    expect(page.teams()).toEqual([team]);
  });

  it('offers a localized retry after request errors', async () => {
    getTeamsPage.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    const fixture = TestBed.createComponent(TeamsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const retry = fixture.nativeElement.querySelector('button');
    expect(retry.textContent).toContain(TestBed.inject(I18nService).t('tables.retry'));
    retry.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.error()).toBe(false);
    expect(getTeamsPage).toHaveBeenCalledTimes(2);
  });

  it('creates a team and navigates to it without appending to the catalog page', () => {
    const page = TestBed.createComponent(TeamsPage).componentInstance;
    page.openCreateTeam();
    page.createTeamForm.setValue({ name: ' New team ', description: ' Description ' });
    page.createTeam();
    expect(createTeam).toHaveBeenCalledWith({ name: 'New team', description: 'Description' });
    expect(navigate).toHaveBeenCalledWith(['/team', 'new-team']);
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe('new-team');
    expect(page.createTeamOpen()).toBe(false);
    expect(page.teams()).toEqual([team]);
  });
});
