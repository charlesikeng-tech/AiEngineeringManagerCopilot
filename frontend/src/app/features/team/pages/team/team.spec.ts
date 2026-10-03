import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BarChartOutline, DeleteOutline, EditOutline, PlusOutline, TeamOutline } from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { of } from 'rxjs';

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
  const getMembers = vi.fn(() => of([member]));
  const updateTeam = vi.fn(() => of({ ...team, name: 'Updated team' }));
  const createMember = vi.fn(() => of({ ...member, id: 'new-member' }));
  const updateMember = vi.fn(() => of({ ...member, name: 'Updated member' }));
  const deleteMember = vi.fn(() => of(undefined));
  const calculate = vi.fn(() => of([]));
  const generateReport = vi.fn(() => of({ id: 'report-id' }));

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [TeamPage],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: teamEn, fr: teamFr }),
        provideNzIcons([BarChartOutline, DeleteOutline, EditOutline, PlusOutline, TeamOutline]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ teamId }) } } },
        { provide: TeamApi, useValue: { getTeam, getMembers, updateTeam, createMember, updateMember, deleteMember } },
        { provide: ReportsApi, useValue: { generateReport } },
        { provide: MetricsApi, useValue: { calculate } },
      ],
    });
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
    expect(getMembers).toHaveBeenCalledWith(teamId);
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
    page.saveMember();
    expect(updateMember).toHaveBeenCalledWith(teamId, member.id, {
      name: 'Updated member', email: member.email, role: 'Developer', providerUserId: null,
    });
    expect(page.members()[0].name).toBe('Updated member');
    page.deleteMember(member);
    expect(deleteMember).toHaveBeenCalledWith(teamId, member.id);
    expect(page.members()).toEqual([]);
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
});
