import { OverlayContainer } from '@angular/cdk/overlay';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import {
  BarChartOutline,
  DeleteOutline,
  EditOutline,
  GithubOutline,
  PlusOutline,
  SlackOutline,
  SyncOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { of, throwError } from 'rxjs';

import teamEn from '../../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../../public/i18n/team/fr.json';
import { GitHubApi } from '../../services/github-api';
import { SlackApi } from '../../services/slack-api';
import { MetricsApi } from '../../services/metrics-api';
import { TeamApi } from '../../services/team-api';
import { ReportsApi } from '@features/reports/services/reports-api';
import { TeamPage } from './team';

describe('team localization', () => {
  const teamId = '11111111-1111-1111-1111-111111111111';
  const createConnection = vi.fn(() =>
    of({
      id: 'connection-id',
      teamId,
      owner: 'charlesikeng-tech',
      ownerType: 'Organization',
      createdAt: '2026-09-01T00:00:00Z',
      lastSyncAt: null,
    }),
  );
  const createSlackConnection = vi.fn(() =>
    of({ teamId, createdAt: '2026-09-01T00:00:00Z' }),
  );

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    createConnection.mockClear();
    createSlackConnection.mockClear();
    TestBed.configureTestingModule({
      imports: [TeamPage],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: teamEn, fr: teamFr }),
        provideNzIcons([
          BarChartOutline,
          DeleteOutline,
          EditOutline,
          GithubOutline,
          PlusOutline,
          SlackOutline,
          SyncOutline,
        ]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ teamId }) } },
        },
        {
          provide: TeamApi,
          useValue: {
            getTeam: () =>
              of({
                id: teamId,
                ownerUserId: teamId,
                name: 'Example team',
                description: null,
                createdAt: '2026-09-01T00:00:00Z',
              }),
            getMembers: () => of([]),
          },
        },
        {
          provide: GitHubApi,
          useValue: {
            getConnection: () => throwError(() => new HttpErrorResponse({ status: 404 })),
            createConnection,
          },
        },
        {
          provide: SlackApi,
          useValue: {
            getConnection: () => throwError(() => new HttpErrorResponse({ status: 404 })),
            createConnection: createSlackConnection,
          },
        },
        { provide: ReportsApi, useValue: {} },
        { provide: MetricsApi, useValue: {} },
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('renders the GitHub drawer and changes labels without clearing entered values', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.openGitHubConnection();
    fixture.detectChanges();
    await fixture.whenStable();

    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    expect(overlay.textContent).toContain(teamEn.team.githubAccount);
    expect(overlay.querySelector('input[formControlName="owner"]')).not.toBeNull();
    fixture.componentInstance.githubConnectionForm.patchValue({
      owner: 'charlesikeng-tech',
      ownerType: 'Organization',
    });

    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(overlay.textContent).toContain(teamFr.team.githubAccount);
    expect(fixture.componentInstance.githubConnectionForm.controls.owner.value)
      .toBe('charlesikeng-tech');
    expect(fixture.componentInstance.githubConnectionForm.controls.ownerType.value)
      .toBe('Organization');
  });

  it('keeps API identifiers unchanged when submitting a French form', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.componentInstance.githubConnectionForm.setValue({
      owner: 'charlesikeng-tech',
      ownerType: 'Organization',
      accessToken: 'fake-test-token-not-a-real-secret',
    });

    fixture.componentInstance.connectGitHub();

    expect(createConnection).toHaveBeenCalledWith(teamId, {
      owner: 'charlesikeng-tech',
      ownerType: 'Organization',
      accessToken: 'fake-test-token-not-a-real-secret',
    });
    expect(fixture.componentInstance.githubConnectionForm.controls.accessToken.value).toBe('');
  });

  it('keeps the Slack webhook private while switching language and saving it', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const secretUrl = 'https://hooks.slack.com/services/T000/B000/test-token';
    fixture.componentInstance.slackWebhookForm.controls.webhookUrl.setValue(secretUrl);

    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(teamFr.team.slackWebhookUrl);
    expect(fixture.componentInstance.slackWebhookForm.controls.webhookUrl.value).toBe(secretUrl);

    fixture.componentInstance.connectSlackWebhook();

    expect(createSlackConnection).toHaveBeenCalledWith(teamId, { webhookUrl: secretUrl });
    expect(fixture.componentInstance.slackWebhookForm.controls.webhookUrl.value).toBe('');
  });
});
