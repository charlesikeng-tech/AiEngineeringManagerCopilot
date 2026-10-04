import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import {
  BarChartOutline,
  DeleteOutline,
  EditOutline,
  PlusOutline,
  TeamOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { TeamContext } from '@core/team/team-context';
import { PagedResult } from '@shared/pagination/paged-result';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { TeamMember } from '@domains/teams/models/team-member';
import { ReportsApi } from '@features/reports/services/reports-api';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of } from 'rxjs';
import teamEn from '../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../public/i18n/team/fr.json';
import { MetricsApi } from '../services/metrics-api';

export { teamEn, teamFr };
export const teamId = '11111111-1111-1111-1111-111111111111';
export const team = {
  id: teamId,
  ownerUserId: teamId,
  name: 'Example team',
  description: null,
  createdAt: '2026-09-01T00:00:00Z',
};
export const member: TeamMember = {
  id: 'member-id',
  teamId,
  name: 'Example member',
  email: 'member@example.com',
  role: 'Developer',
  providerUserId: null,
  createdAt: '2026-09-01T00:00:00Z',
};
export const memberPage = (
  items: readonly TeamMember[] = [member],
  totalCount = 31,
): PagedResult<TeamMember> => ({ items, totalCount, pageNumber: 1, pageSize: 10 });

export function configureTeamTests(component: Type<unknown>) {
  localStorage.removeItem('selectedTeamId');
  const api = {
    getTeam: vi.fn<TeamApi['getTeam']>(() => of(team)),
    getMembers: vi.fn<TeamApi['getMembers']>(() => of([member])),
    getMembersPage: vi.fn<TeamApi['getMembersPage']>(() => of(memberPage())),
    updateTeam: vi.fn<TeamApi['updateTeam']>(() => of({ ...team, name: 'Updated team' })),
    createMember: vi.fn<TeamApi['createMember']>(() => of({ ...member, id: 'new-member' })),
    updateMember: vi.fn<TeamApi['updateMember']>(() => of({ ...member, name: 'Updated member' })),
    deleteMember: vi.fn<TeamApi['deleteMember']>(() => of(undefined)),
    deleteTeam: vi.fn<TeamApi['deleteTeam']>(() => of(undefined)),
  };
  const calculate = vi.fn<MetricsApi['calculate']>(() => of([]));
  const generateReport = vi.fn(() => of({ id: 'report-id' }));
  const message = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
  TestBed.configureTestingModule({
    imports: [component],
    providers: [
      provideRouter([]),
      ...provideI18nTesting('en', { en: teamEn, fr: teamFr }),
      provideNzIcons([BarChartOutline, DeleteOutline, EditOutline, PlusOutline, TeamOutline]),
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap({ teamId }) } },
      },
      { provide: TeamApi, useValue: api },
      { provide: ReportsApi, useValue: { generateReport } },
      { provide: MetricsApi, useValue: { calculate } },
      { provide: NzMessageService, useValue: message },
    ],
  });
  vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  return { api, calculate, generateReport, message };
}

export async function teamWidgetFixture<T>(component: Type<T>) {
  const context = TestBed.inject(TeamContext);
  context.selectTeam(teamId);
  const fixture = TestBed.createComponent(component);
  fixture.componentRef.setInput('teamId', teamId);
  fixture.componentRef.setInput('selectionVersion', context.selectionVersion());
  fixture.detectChanges();
  await fixture.whenStable();
  return fixture;
}
