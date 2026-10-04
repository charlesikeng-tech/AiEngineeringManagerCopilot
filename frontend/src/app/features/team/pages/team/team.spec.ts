import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BarChartOutline, DeleteOutline, EditOutline, PlusOutline, TeamOutline } from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, Subject, throwError } from 'rxjs';
import { TeamContext } from '@core/team/team-context';
import { PagedResult } from '@core/models/paged-result';
import { TeamMember } from '../../models/team.model';
import { EngineeringMetric } from '../../models/engineering-metric';

import teamEn from '../../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../../public/i18n/team/fr.json';
import { ReportsApi } from '@features/reports/services/reports-api';
import { MetricsApi } from '../../services/metrics-api';
import { TeamApi } from '../../services/team-api';
import { TeamPage } from './team';

describe('team management', () => {
  const teamId = '11111111-1111-1111-1111-111111111111';
  const team = {
    id: teamId,
    ownerUserId: teamId,
    name: 'Example team',
    description: null,
    createdAt: '2026-09-01T00:00:00Z',
  };
  const member = {
    id: 'member-id',
    teamId,
    name: 'Example member',
    email: 'member@example.com',
    role: 'Developer' as const,
    providerUserId: null,
    createdAt: '2026-09-01T00:00:00Z',
  };
  const getTeam = vi.fn(() => of(team));
  const memberPage = (items: readonly TeamMember[] = [member], totalCount = 31): PagedResult<TeamMember> =>
    ({ items, totalCount, pageNumber: 1, pageSize: 10 });
  const getMembers = vi.fn(() => of([member]));
  const getMembersPage = vi.fn<(...args: [string, number, number]) => Observable<PagedResult<TeamMember>>>();
  const updateTeam = vi.fn(() => of({ ...team, name: 'Updated team' }));
  const createMember = vi.fn(() => of({ ...member, id: 'new-member' }));
  const updateMember = vi.fn(() => of({ ...member, name: 'Updated member' }));
  const deleteMember = vi.fn(() => of(undefined));
  const deleteTeam = vi.fn(() => of(undefined));
  const calculate = vi.fn<(...args: [string, string, string]) => Observable<readonly EngineeringMetric[]>>();
  const generateReport = vi.fn(() => of({ id: 'report-id' }));

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    vi.clearAllMocks();
    getMembersPage.mockReset().mockReturnValue(of(memberPage()));
    calculate.mockReset().mockReturnValue(of([]));
    TestBed.configureTestingModule({
      imports: [TeamPage],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: teamEn, fr: teamFr }),
        provideNzIcons([BarChartOutline, DeleteOutline, EditOutline, PlusOutline, TeamOutline]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ teamId }) } } },
        { provide: TeamApi, useValue: { getTeam, getMembers, getMembersPage, updateTeam, createMember, updateMember, deleteMember, deleteTeam } },
        { provide: ReportsApi, useValue: { generateReport } },
        { provide: MetricsApi, useValue: { calculate } },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('loads team management and metrics without integration services or forms', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(getTeam).toHaveBeenCalledWith(teamId);
    expect(getMembersPage).toHaveBeenCalledWith(teamId, 1, 10);
    expect(getMembers).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(team.name);
    expect(fixture.nativeElement.textContent).toContain(member.name);
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.engineeringData);
    expect(fixture.nativeElement.querySelector('.data-source')).toBeNull();
    expect(fixture.nativeElement.querySelector('input[formControlName="webhookUrl"]')).toBeNull();
    expect('githubConnectionForm' in fixture.componentInstance).toBe(false);
    expect('slackWebhookForm' in fixture.componentInstance).toBe(false);
    expect('microsoftTeamsWebhookForm' in fixture.componentInstance).toBe(false);
  });

  it('preserves entered team and member values when changing language', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.openEditTeam();
    page.editTeamForm.controls.name.setValue('Updated team');
    page.openEditMember(member);
    page.addMemberForm.controls.name.setValue('Updated member');

    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(page.editTeamForm.controls.name.value).toBe('Updated team');
    expect(page.addMemberForm.controls.name.value).toBe('Updated member');
    expect(page.roleLabel('Developer')).toBe(teamFr.team.roles.Developer);
  });

  it('updates team information and manages members', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.openEditTeam();
    page.editTeamForm.setValue({ name: ' Updated team ', description: ' Description ' });
    page.updateTeam();
    expect(updateTeam).toHaveBeenCalledWith(teamId, { name: 'Updated team', description: 'Description' });
    expect(page.editTeamOpen()).toBe(false);

    page.openEditMember(member);
    page.addMemberForm.controls.name.setValue('Updated member');
    getMembersPage.mockReturnValueOnce(of(memberPage([{ ...member, name: 'Updated member' }])));
    page.saveMember();
    expect(updateMember).toHaveBeenCalledWith(teamId, member.id, {
      name: 'Updated member', email: member.email, role: 'Developer', providerUserId: null,
    });
    expect(page.members()[0].name).toBe('Updated member');
    getMembersPage.mockReturnValueOnce(of(memberPage([], 0)));
    page.deleteMember(member);
    expect(deleteMember).toHaveBeenCalledWith(teamId, member.id);
    expect(page.members()).toEqual([]);
    expect(getMembersPage).toHaveBeenCalledTimes(3);
  });

  it('renders the server member total and identifying information in a compact table', async () => {
    getMembersPage.mockReturnValue(of(memberPage([{ ...member, providerUserId: 'github-user' }])));
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.memberPagination.totalCount()).toBe(31);
    expect(fixture.nativeElement.querySelector('.page-summary__value').textContent).toContain('31');
    expect(fixture.nativeElement.querySelector('.member-total').textContent).toContain('31');
    const table = fixture.nativeElement.querySelector('table.app-data-table');
    expect(table.textContent).toContain(member.email);
    expect(table.textContent).toContain('github-user');
    expect(table.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(table.querySelector('caption').textContent).toContain(teamEn.team.membersTitle);
  });

  it('requests only the next page and resets to page one when choosing size twenty', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.changeMemberPage(2);
    expect(getMembersPage).toHaveBeenLastCalledWith(teamId, 2, 10);
    page.changeMemberPageSize(20);
    expect(getMembersPage).toHaveBeenLastCalledWith(teamId, 1, 20);
    page.changeMemberPageSize(20);
    expect(getMembersPage).toHaveBeenCalledTimes(3);
    expect(getMembers).not.toHaveBeenCalled();
  });

  it('cancels superseded member page loads and resets pagination on selection change and clear', async () => {
    const cancelled = vi.fn();
    getMembersPage.mockReturnValueOnce(new Observable(() => cancelled));
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    const context = TestBed.inject(TeamContext);
    const otherId = 'other-team';
    getTeam.mockReturnValueOnce(of({ ...team, id: otherId }));
    context.selectTeam(otherId);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(cancelled).toHaveBeenCalledOnce();
    expect(getMembersPage).toHaveBeenLastCalledWith(otherId, 1, 10);
    page.changeMemberPage(2);
    const clearCancelled = vi.fn();
    getMembersPage.mockReturnValueOnce(new Observable(() => clearCancelled));
    page.loadMembers();
    context.clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(clearCancelled).toHaveBeenCalledOnce();
    expect(page.members()).toEqual([]);
    expect(page.team()).toBeNull();
    expect(page.memberPagination.pageNumber()).toBe(1);
    expect(page.memberPagination.totalCount()).toBe(0);
    const calls = getMembersPage.mock.calls.length;
    page.loadMembers();
    expect(getMembersPage).toHaveBeenCalledTimes(calls);
  });

  it('cancels an older page request when changing page', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const cancelled = vi.fn();
    getMembersPage.mockReturnValueOnce(new Observable(() => cancelled));
    fixture.componentInstance.loadMembers();
    fixture.componentInstance.changeMemberPage(2);
    expect(cancelled).toHaveBeenCalledOnce();
    expect(getMembersPage).toHaveBeenLastCalledWith(teamId, 2, 10);
  });

  it('cancels an outstanding team load when the selection is cleared', async () => {
    const cancelled = vi.fn();
    getTeam.mockReturnValueOnce(new Observable(() => cancelled));
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(cancelled).toHaveBeenCalledOnce();
    expect(getMembersPage).not.toHaveBeenCalled();
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('retries a failed team load for the same route-selected team', async () => {
    getTeam.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.error()).toBe(true);
    fixture.componentInstance.retryTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getTeam).toHaveBeenCalledTimes(2);
    expect(getMembersPage).toHaveBeenCalledExactlyOnceWith(teamId, 1, 10);
    expect(fixture.componentInstance.team()).toEqual(team);
  });

  it('refreshes the current page after creating a member, including provider ID', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.changeMemberPage(2);
    page.openAddMember();
    page.addMemberForm.setValue({ name: ' New member ', email: 'new@example.com', role: 'QA', providerUserId: ' provider-user ' });
    page.saveMember();
    expect(createMember).toHaveBeenCalledWith(teamId, {
      name: 'New member', email: 'new@example.com', role: 'QA', providerUserId: 'provider-user',
    });
    expect(getMembersPage).toHaveBeenLastCalledWith(teamId, 2, 10);
    expect(getMembersPage).toHaveBeenCalledTimes(3);
    expect(page.addMemberOpen()).toBe(false);
  });

  it('clamps and refetches after deletion empties the last member page', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.changeMemberPage(2);
    getMembersPage.mockReturnValueOnce(of(memberPage([], 10)));
    page.deleteMember(member);
    expect(getMembersPage.mock.calls.slice(-2)).toEqual([[teamId, 2, 10], [teamId, 1, 10]]);
    expect(page.memberPagination.pageNumber()).toBe(1);
    expect(page.members()).toEqual([member]);
  });

  it.each(['create', 'update', 'delete'] as const)('ignores stale %s callbacks even when the selected team returns to the same ID', async (operation) => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    const pending = new Subject<typeof member>();
    const pendingDelete = new Subject<undefined>();
    if (operation === 'delete') {
      deleteMember.mockReturnValueOnce(pendingDelete);
      page.deleteMember(member);
    } else {
      (operation === 'create' ? createMember : updateMember).mockReturnValueOnce(pending);
      if (operation === 'create') {
        page.openAddMember();
        page.addMemberForm.setValue({ name: member.name, email: member.email, role: member.role, providerUserId: '' });
      } else {
        page.openEditMember(member);
      }
      page.saveMember();
    }
    const context = TestBed.inject(TeamContext);
    context.selectTeam('another-team');
    context.selectTeam(teamId);
    const callCount = getMembersPage.mock.calls.length;
    pending.next(member);
    pendingDelete.next(undefined);
    expect(getMembersPage).toHaveBeenCalledTimes(callCount);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.creatingMember()).toBe(false);
    expect(page.deletingMemberId()).toBeNull();
    expect(page.addMemberOpen()).toBe(false);
    expect(page.memberPagination.pageNumber()).toBe(1);
  });

  it('does not send stale member forms or deletion to a newly selected team', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.openEditMember(member);
    TestBed.inject(TeamContext).selectTeam('another-team');
    page.saveMember();
    page.deleteMember(member);
    expect(updateMember).not.toHaveBeenCalled();
    expect(deleteMember).not.toHaveBeenCalled();
  });

  it.each(['update', 'delete'] as const)('ignores stale team %s completion without changing the newer selection', async (operation) => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    const pendingUpdate = new Subject<typeof team>();
    const pendingDelete = new Subject<undefined>();
    if (operation === 'update') {
      updateTeam.mockReturnValueOnce(pendingUpdate);
      page.openEditTeam();
      page.updateTeam();
      expect(updateTeam).toHaveBeenCalledWith(teamId, { name: team.name, description: null });
    } else {
      deleteTeam.mockReturnValueOnce(pendingDelete);
      page.deleteTeam();
      expect(deleteTeam).toHaveBeenCalledWith(teamId);
    }
    const context = TestBed.inject(TeamContext);
    context.selectTeam('new-selection');
    pendingUpdate.next(team);
    pendingDelete.next(undefined);
    expect(context.selectedTeamId()).toBe('new-selection');
    expect(TestBed.inject(Router).navigate).not.toHaveBeenCalled();
  });

  it('allows retry after a member page error without discarding the team or metrics', async () => {
    getMembersPage.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.membersError()).toBe(true);
    expect(fixture.componentInstance.team()).toEqual(team);
    const retry = Array.from(fixture.nativeElement.querySelectorAll('button')).find((button) => (button as HTMLElement).textContent?.includes(TestBed.inject(I18nService).t('tables.retry'))) as HTMLButtonElement;
    expect(retry).toBeDefined();
    retry.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.membersError()).toBe(false);
    expect(getTeam).toHaveBeenCalledTimes(1);
  });

  it('calculates metrics for the selected team without fetching integration configuration', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.metricsPeriodForm.setValue({ periodStart: '2026-09-01', periodEnd: '2026-09-30' });
    page.calculateMetrics();
    expect(calculate).toHaveBeenCalledWith(teamId, '2026-09-01', '2026-09-30');
    expect(page.calculatingMetrics()).toBe(false);
    expect(page.calculatedMetricsPeriod()).toEqual({ periodStart: '2026-09-01', periodEnd: '2026-09-30' });
    page.generateReport();
    expect(generateReport).not.toHaveBeenCalled();
  });

  it('preserves metric/report generation while member pagination changes', async () => {
    calculate.mockReturnValueOnce(of([{
      id: 'metric-id', teamId, metricType: 'OpenPRs', value: 3,
      dataStatus: 'Available', periodStart: '2026-09-01', periodEnd: '2026-09-30',
      createdAt: '2026-10-01T00:00:00Z',
    }]));
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.metricsPeriodForm.setValue({ periodStart: '2026-09-01', periodEnd: '2026-09-30' });
    page.calculateMetrics();
    page.changeMemberPage(2);
    expect(page.hasValidCalculatedMetrics()).toBe(true);
    expect(page.availableMetricCount()).toBe(1);
    page.generateReport();
    expect(generateReport).toHaveBeenCalledWith(teamId, '2026-09-01', '2026-09-30');
  });
});
