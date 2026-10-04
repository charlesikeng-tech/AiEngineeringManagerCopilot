import { HttpErrorResponse } from '@angular/common/http';
import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  GithubOutline,
  SlackOutline,
  SyncOutline,
  TeamOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { TeamContext } from '@core/team/team-context';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, throwError } from 'rxjs';
import commonEn from '../../../../../public/i18n/common/en.json';
import commonFr from '../../../../../public/i18n/common/fr.json';
import teamEn from '../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../public/i18n/team/fr.json';
import { GitHubApi } from '../providers/github/data-access/github-api';
import { JiraApi } from '../providers/jira/data-access/jira-api';
import { SlackApi } from '../providers/slack/data-access/slack-api';
import { MicrosoftTeamsApi } from '../providers/microsoft-teams/data-access/microsoft-teams-api';

export { commonEn, commonFr, teamEn, teamFr };
export const teamId = '11111111-1111-1111-1111-111111111111';
export const githubConnection = {
  id: 'connection-id',
  teamId,
  owner: 'charlesikeng-tech',
  ownerType: 'Organization' as const,
  createdAt: '2026-09-01T00:00:00Z',
  lastSyncAt: null,
};
export const webhookConnection = { teamId, createdAt: '2026-09-01T00:00:00Z' };
export const slackUrl = 'https://hooks.slack.com/services/T000/B000/test-token';
export const microsoftTeamsUrl =
  'https://example.environment.api.powerplatform.com/powerautomate/automations/direct/workflows/workflow-id/triggers/manual/paths/invoke?sig=fake-test-signature';

export function configureIntegrationTests(component: Type<unknown>) {
  localStorage.removeItem('selectedTeamId');
  const missing = () => throwError(() => new HttpErrorResponse({ status: 404 }));
  const github = {
    getConnection: vi.fn<GitHubApi['getConnection']>(missing),
    createConnection: vi.fn<GitHubApi['createConnection']>(() => of(githubConnection)),
    testConnection: vi.fn<GitHubApi['testConnection']>(() =>
      of({ success: true, message: 'Verified.', organization: 'Example' }),
    ),
    sync: vi.fn<GitHubApi['sync']>(() => of({ synchronized: 3, created: 2, updated: 1 })),
    deleteConnection: vi.fn<GitHubApi['deleteConnection']>(() => of(undefined)),
  };
  const slack = {
    getConnection: vi.fn<SlackApi['getConnection']>(missing),
    createConnection: vi.fn<SlackApi['createConnection']>(() => of(webhookConnection)),
    testConnection: vi.fn<SlackApi['testConnection']>(() =>
      of({ success: true, message: 'Sent.' }),
    ),
    deleteConnection: vi.fn<SlackApi['deleteConnection']>(() => of(undefined)),
  };
  const microsoftTeams = {
    getConnection: vi.fn<MicrosoftTeamsApi['getConnection']>(missing),
    createConnection: vi.fn<MicrosoftTeamsApi['createConnection']>(() => of(webhookConnection)),
    testConnection: vi.fn<MicrosoftTeamsApi['testConnection']>(() =>
      of({ success: true, message: 'Notification sent.' }),
    ),
    deleteConnection: vi.fn<MicrosoftTeamsApi['deleteConnection']>(() => of(undefined)),
  };
  const message = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
  TestBed.configureTestingModule({
    imports: [component],
    providers: [
      provideRouter([]),
      ...provideI18nTesting('en', {
        en: { ...commonEn, ...teamEn },
        fr: { ...commonFr, ...teamFr },
      }),
      provideNzIcons([GithubOutline, SlackOutline, SyncOutline, TeamOutline]),
      { provide: NzMessageService, useValue: message },
      {
        provide: TeamApi,
        useValue: {
          getTeams: () =>
            of([
              {
                id: teamId,
                ownerUserId: teamId,
                name: 'Example team',
                description: null,
                createdAt: '2026-09-01T00:00:00Z',
              },
            ]),
        },
      },
      { provide: GitHubApi, useValue: github },
      { provide: SlackApi, useValue: slack },
      { provide: MicrosoftTeamsApi, useValue: microsoftTeams },
      { provide: JiraApi, useValue: { getConnection: missing } },
    ],
  });
  return { github, slack, microsoftTeams, message };
}

export async function integrationFixture<T>(component: Type<T>) {
  TestBed.inject(TeamContext).selectTeam(teamId);
  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();
  await fixture.whenStable();
  return fixture;
}
