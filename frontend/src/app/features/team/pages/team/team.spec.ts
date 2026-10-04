import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { Observable, of, Subject, throwError } from 'rxjs';
import { TeamMembers } from '../../components/team-members/team-members';
import { TeamEngineeringData } from '../../components/team-engineering-data/team-engineering-data';
import { configureTeamTests, member, team, teamEn, teamId } from '../../testing/team-testing';
import { TeamPage } from './team';

describe('team route orchestration', () => {
  let mocks: ReturnType<typeof configureTeamTests>;
  beforeEach(() => {
    mocks = configureTeamTests(TeamPage);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });
  async function fixtureForTeam() {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  it('loads route-selected team, composes member/metrics widgets and displays member totals', async () => {
    const fixture = await fixtureForTeam();
    expect(mocks.api.getTeam).toHaveBeenCalledWith(teamId);
    expect(mocks.api.getMembersPage).toHaveBeenCalledWith(teamId, 1, 10);
    expect(mocks.api.getMembers).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(team.name);
    expect(fixture.nativeElement.textContent).toContain(member.name);
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.engineeringData);
    expect(fixture.nativeElement.querySelector('.data-source')).toBeNull();
    expect(fixture.nativeElement.querySelector('input[formControlName="webhookUrl"]')).toBeNull();
    expect('githubConnectionForm' in fixture.componentInstance).toBe(false);
    expect('slackWebhookForm' in fixture.componentInstance).toBe(false);
    expect('microsoftTeamsWebhookForm' in fixture.componentInstance).toBe(false);
    expect(fixture.nativeElement.querySelector('.page-summary__value').textContent).toContain('31');
    expect(fixture.nativeElement.querySelector('.member-total').textContent).toContain('31');
    const members = fixture.debugElement.query(By.directive(TeamMembers))
      .componentInstance as TeamMembers;
    const metrics = fixture.debugElement.query(By.directive(TeamEngineeringData))
      .componentInstance as TeamEngineeringData;
    expect(members.teamId()).toBe(teamId);
    expect(metrics.teamId()).toBe(teamId);
    expect(members.selectionVersion()).toBe(TestBed.inject(TeamContext).selectionVersion());
  });

  it('preserves entered team values during language changes and submits trimmed information', async () => {
    const fixture = await fixtureForTeam();
    const page = fixture.componentInstance;
    page.openEditTeam();
    page.editTeamForm.setValue({ name: ' Updated team ', description: ' Description ' });
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(page.editTeamForm.controls.name.value).toBe(' Updated team ');
    page.updateTeam();
    expect(mocks.api.updateTeam).toHaveBeenCalledWith(teamId, {
      name: 'Updated team',
      description: 'Description',
    });
    expect(page.editTeamOpen()).toBe(false);
  });

  it('cancels outstanding team loads and does not mount children when selection is cleared', async () => {
    const cancelled = vi.fn();
    mocks.api.getTeam.mockReturnValueOnce(new Observable(() => cancelled));
    const fixture = await fixtureForTeam();
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(cancelled).toHaveBeenCalledOnce();
    expect(mocks.api.getMembersPage).not.toHaveBeenCalled();
    expect(fixture.componentInstance.loading()).toBe(false);
    expect(fixture.debugElement.query(By.directive(TeamMembers))).toBeNull();
    expect(fixture.debugElement.query(By.directive(TeamEngineeringData))).toBeNull();
  });

  it('retries failed route-selected team loads before loading members', async () => {
    mocks.api.getTeam.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    const fixture = await fixtureForTeam();
    expect(fixture.componentInstance.error()).toBe(true);
    fixture.componentInstance.retryTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(mocks.api.getTeam).toHaveBeenCalledTimes(2);
    expect(mocks.api.getMembersPage).toHaveBeenCalledExactlyOnceWith(teamId, 1, 10);
    expect(fixture.componentInstance.team()).toEqual(team);
  });

  it.each(['update', 'delete'] as const)(
    'ignores stale team %s completions without changing the newer selection',
    async (action) => {
      const fixture = await fixtureForTeam();
      const page = fixture.componentInstance;
      const pendingUpdate = new Subject<typeof team>();
      const pendingDelete = new Subject<void>();
      if (action === 'update') {
        mocks.api.updateTeam.mockReturnValueOnce(pendingUpdate);
        page.openEditTeam();
        page.updateTeam();
        expect(mocks.api.updateTeam).toHaveBeenCalledWith(teamId, {
          name: team.name,
          description: null,
        });
      } else {
        mocks.api.deleteTeam.mockReturnValueOnce(pendingDelete);
        page.deleteTeam();
        expect(mocks.api.deleteTeam).toHaveBeenCalledWith(teamId);
      }
      const context = TestBed.inject(TeamContext);
      context.selectTeam('new-selection');
      pendingUpdate.next(team);
      pendingDelete.next();
      expect(context.selectedTeamId()).toBe('new-selection');
      expect(TestBed.inject(Router).navigate).not.toHaveBeenCalled();
    },
  );

  it('keeps team and metrics intact on member page errors and pagination changes', async () => {
    mocks.api.getMembersPage.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    mocks.calculate.mockReturnValueOnce(
      of([
        {
          id: 'metric-id',
          teamId,
          metricType: 'OpenPRs',
          value: 3,
          dataStatus: 'Available',
          periodStart: '2026-09-01',
          periodEnd: '2026-09-30',
          createdAt: '2026-10-01T00:00:00Z',
        },
      ]),
    );
    const fixture = await fixtureForTeam();
    const members = fixture.debugElement.query(By.directive(TeamMembers))
      .componentInstance as TeamMembers;
    const metrics = fixture.debugElement.query(By.directive(TeamEngineeringData))
      .componentInstance as TeamEngineeringData;
    expect(members.membersError()).toBe(true);
    expect(fixture.componentInstance.team()).toEqual(team);
    members.loadMembers();
    metrics.metricsPeriodForm.setValue({ periodStart: '2026-09-01', periodEnd: '2026-09-30' });
    metrics.calculateMetrics();
    members.changeMemberPage(2);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(metrics.hasValidCalculatedMetrics()).toBe(true);
    expect(metrics.availableMetricCount()).toBe(1);
    metrics.generateReport();
    expect(mocks.generateReport).toHaveBeenCalledWith(teamId, '2026-09-01', '2026-09-30');
    expect(mocks.api.getTeam).toHaveBeenCalledTimes(1);
  });

  it('validates team updates and preserves pending drawer and failed actions', async () => {
    const page = (await fixtureForTeam()).componentInstance;
    page.openEditTeam();
    page.editTeamForm.controls.name.setValue('');
    page.updateTeam();
    expect(mocks.api.updateTeam).not.toHaveBeenCalled();
    page.editTeamForm.controls.name.setValue(team.name);
    const pending = new Subject<typeof team>();
    mocks.api.updateTeam.mockReturnValue(pending);
    page.updateTeam();
    page.closeEditTeam();
    expect(page.editTeamOpen()).toBe(true);
    pending.error(new Error('unavailable'));
    expect(page.updatingTeam()).toBe(false);
    expect(page.editTeamOpen()).toBe(true);
    mocks.api.deleteTeam.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    page.deleteTeam();
    expect(page.deletingTeam()).toBe(false);
    expect(mocks.message.error).toHaveBeenCalledTimes(2);
  });

  it('clears selected team and navigates after successful deletion', async () => {
    const page = (await fixtureForTeam()).componentInstance;
    page.deleteTeam();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBeNull();
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/team']);
    expect(mocks.message.success).toHaveBeenCalled();
  });
});
