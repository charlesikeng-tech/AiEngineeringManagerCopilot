import { OverlayContainer } from '@angular/cdk/overlay';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  BarChartOutline,
  DeleteOutline,
  EditOutline,
  GithubOutline,
  PlusOutline,
  SlackOutline,
  TeamOutline,
  SyncOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Observable, of, Subject, throwError } from 'rxjs';

import teamEn from '../../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../../public/i18n/team/fr.json';
import commonEn from '../../../../../../public/i18n/common/en.json';
import commonFr from '../../../../../../public/i18n/common/fr.json';
import { GitHubApi } from '../../../team/services/github-api';
import { SlackApi } from '../../../team/services/slack-api';
import { MicrosoftTeamsApi } from '../../../team/services/microsoft-teams-api';
import { MicrosoftTeamsWebhookConnection } from '../../../team/models/microsoft-teams-webhook-connection';
import { TestMicrosoftTeamsWebhookResponse } from '../../../team/models/test-microsoft-teams-webhook-response';
import { GitHubConnection } from '../../../team/models/github-connection';
import { GitHubConnectionTestResponse } from '../../../team/models/github-connection-test-response';
import { GitHubSyncResponse } from '../../../team/models/github-sync-response';
import { SlackWebhookConnection } from '../../../team/models/slack-webhook-connection';
import { TestSlackWebhookResponse } from '../../../team/models/test-slack-webhook-response';
import { JiraApi } from '../../services/jira-api';
import { IntegrationSettings as IntegrationsPage } from './integration-settings';

describe('team-scoped integrations', () => {
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
  const createSlackConnection = vi.fn(() => of({ teamId, createdAt: '2026-09-01T00:00:00Z' }));
  const microsoftTeamsConnection = { teamId, createdAt: '2026-09-01T00:00:00Z' };
  const microsoftTeamsUrl =
    'https://example.environment.api.powerplatform.com/powerautomate/automations/direct/workflows/workflow-id/triggers/manual/paths/invoke?sig=fake-test-signature';
  const getMicrosoftTeamsConnection = vi.fn<() => Observable<MicrosoftTeamsWebhookConnection>>();
  const createMicrosoftTeamsConnection = vi.fn<() => Observable<MicrosoftTeamsWebhookConnection>>();
  const testMicrosoftTeamsConnection = vi.fn<() => Observable<TestMicrosoftTeamsWebhookResponse>>();
  const deleteMicrosoftTeamsConnection = vi.fn<() => Observable<void>>();
  const githubConnection: GitHubConnection = {
    id: 'connection-id',
    teamId,
    owner: 'charlesikeng-tech',
    ownerType: 'Organization',
    createdAt: '2026-09-01T00:00:00Z',
    lastSyncAt: null,
  };
  const getGitHubConnection = vi.fn<() => Observable<GitHubConnection>>();
  const testGitHubConnection = vi.fn<() => Observable<GitHubConnectionTestResponse>>();
  const syncGitHub = vi.fn<() => Observable<GitHubSyncResponse>>();
  const deleteGitHubConnection = vi.fn<() => Observable<void>>();
  const getSlackConnection = vi.fn<() => Observable<SlackWebhookConnection>>();
  const testSlackConnection = vi.fn<() => Observable<TestSlackWebhookResponse>>();
  const deleteSlackConnection = vi.fn<() => Observable<void>>();

  it('rejects same-tick stale responses and resets secrets after returning to the original team', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const pending = new Subject<TestMicrosoftTeamsWebhookResponse>();
    getMicrosoftTeamsConnection.mockReturnValue(of(microsoftTeamsConnection));
    testMicrosoftTeamsConnection.mockReturnValue(pending);
    const page = fixture.componentInstance;
    page.reloadMicrosoftTeamsConnection();
    page.slackWebhookForm.controls.webhookUrl.setValue('https://hooks.slack.com/services/a/b/fake');
    page.testMicrosoftTeamsWebhook();
    const context = TestBed.inject(TeamContext);
    context.selectTeam('other-team');
    context.selectTeam(teamId);
    pending.next({ success: true, message: 'Outdated' });
    expect(page.microsoftTeamsTestResult()).toBeNull();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.slackWebhookForm.controls.webhookUrl.value).toBe('');
    expect(page.microsoftTeamsTesting()).toBe(false);
    expect(pending.observed).toBe(false);
  });

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    createConnection.mockClear();
    createSlackConnection.mockClear();
    getGitHubConnection
      .mockReset()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    testGitHubConnection
      .mockReset()
      .mockReturnValue(of({ success: true, message: 'Verified.', organization: 'Example' }));
    syncGitHub.mockReset().mockReturnValue(of({ synchronized: 3, created: 2, updated: 1 }));
    deleteGitHubConnection.mockReset().mockReturnValue(of(undefined));
    getSlackConnection
      .mockReset()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    testSlackConnection.mockReset().mockReturnValue(of({ success: true, message: 'Sent.' }));
    deleteSlackConnection.mockReset().mockReturnValue(of(undefined));
    getMicrosoftTeamsConnection
      .mockReset()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    createMicrosoftTeamsConnection.mockReset().mockReturnValue(of(microsoftTeamsConnection));
    testMicrosoftTeamsConnection
      .mockReset()
      .mockReturnValue(of({ success: true, message: 'Notification sent.' }));
    deleteMicrosoftTeamsConnection.mockReset().mockReturnValue(of(undefined));
    TestBed.configureTestingModule({
      imports: [IntegrationsPage],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', {
          en: { ...commonEn, ...teamEn },
          fr: { ...commonFr, ...teamFr },
        }),
        provideNzIcons([
          BarChartOutline,
          DeleteOutline,
          EditOutline,
          GithubOutline,
          PlusOutline,
          SlackOutline,
          TeamOutline,
          SyncOutline,
        ]),
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
        {
          provide: GitHubApi,
          useValue: {
            getConnection: getGitHubConnection,
            createConnection,
            testConnection: testGitHubConnection,
            sync: syncGitHub,
            deleteConnection: deleteGitHubConnection,
          },
        },
        {
          provide: SlackApi,
          useValue: {
            getConnection: getSlackConnection,
            createConnection: createSlackConnection,
            testConnection: testSlackConnection,
            deleteConnection: deleteSlackConnection,
          },
        },
        {
          provide: MicrosoftTeamsApi,
          useValue: {
            getConnection: getMicrosoftTeamsConnection,
            createConnection: createMicrosoftTeamsConnection,
            testConnection: testMicrosoftTeamsConnection,
            deleteConnection: deleteMicrosoftTeamsConnection,
          },
        },
        {
          provide: JiraApi,
          useValue: {
            getConnection: () => throwError(() => new HttpErrorResponse({ status: 404 })),
          },
        },
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('renders the GitHub drawer and changes labels without clearing entered values', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
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
    expect(fixture.componentInstance.githubConnectionForm.controls.owner.value).toBe(
      'charlesikeng-tech',
    );
    expect(fixture.componentInstance.githubConnectionForm.controls.ownerType.value).toBe(
      'Organization',
    );
  });

  it('groups GitHub and Jira as data sources and webhooks as notifications', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const sections: HTMLElement[] = [
      ...fixture.nativeElement.querySelectorAll('section.app-section'),
    ];
    const sources = sections.find((section) => section.querySelector('app-jira-integration'))!;
    const notifications = sections.find((section) => section.querySelector('#slack-webhook-url'))!;
    expect(fixture.nativeElement.querySelector('app-team-selector')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain(commonEn.integrations.scope);
    expect(sources.textContent).toContain(teamEn.team.github);
    expect(sources.textContent).not.toContain(teamEn.team.slack);
    expect(
      sources.querySelector('app-jira-integration')!.previousElementSibling!.textContent,
    ).toContain(teamEn.team.github);
    expect(notifications.textContent).toContain(commonEn.integrations.notifications);
    expect(notifications.textContent).toContain(teamEn.team.slack);
    expect(notifications.textContent).toContain(teamEn.team.microsoftTeams);
  });

  it('does not fetch connections or show forms without a selected team', async () => {
    TestBed.overrideProvider(TeamApi, { useValue: { getTeams: () => of([]) } });
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain(commonEn.integrations.noTeamDescription);
    expect(fixture.nativeElement.querySelector('app-team-selector')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(getGitHubConnection).not.toHaveBeenCalled();
    expect(getSlackConnection).not.toHaveBeenCalled();
    expect(getMicrosoftTeamsConnection).not.toHaveBeenCalled();
  });

  it('tests, synchronizes and disconnects GitHub for the selected team', async () => {
    getGitHubConnection.mockReturnValue(of(githubConnection));
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.testGitHubConnection();
    expect(testGitHubConnection).toHaveBeenCalledWith(teamId);
    expect(page.githubTestResult()?.success).toBe(true);
    page.syncGitHub();
    expect(syncGitHub).toHaveBeenCalledWith(teamId);
    expect(page.githubSyncResult()).toEqual({ synchronized: 3, created: 2, updated: 1 });
    expect(getGitHubConnection).toHaveBeenCalledTimes(2);
    page.disconnectGitHub();
    expect(deleteGitHubConnection).toHaveBeenCalledWith(teamId);
    expect(page.githubConnection()).toBeNull();
    expect(page.githubTestResult()).toBeNull();
    expect(page.githubSyncResult()).toBeNull();
  });

  it('loads, tests and disconnects Slack independently of the other integrations', async () => {
    getSlackConnection.mockReturnValue(of({ teamId, createdAt: '2026-09-01T00:00:00Z' }));
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.testSlackWebhook();
    expect(testSlackConnection).toHaveBeenCalledWith(teamId);
    expect(page.slackTestResult()?.success).toBe(true);
    expect(page.microsoftTeamsTestResult()).toBeNull();
    page.disconnectSlackWebhook();
    expect(deleteSlackConnection).toHaveBeenCalledWith(teamId);
    expect(page.slackConnection()).toBeNull();
    expect(page.slackTestResult()).toBeNull();
  });

  it('retries GitHub and Slack load errors while treating missing connections normally', async () => {
    getGitHubConnection.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    getSlackConnection.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    expect(page.githubConnectionError()).toBe(true);
    expect(page.slackConnectionError()).toBe(true);
    expect(page.microsoftTeamsConnectionError()).toBe(false);
    getGitHubConnection.mockReturnValue(of(githubConnection));
    getSlackConnection.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    page.reloadGitHubConnection();
    page.reloadSlackConnection();
    expect(page.githubConnection()).toEqual(githubConnection);
    expect(page.githubConnectionError()).toBe(false);
    expect(page.slackConnection()).toBeNull();
    expect(page.slackConnectionError()).toBe(false);
    expect(page.slackConnectionLoading()).toBe(false);
  });

  it('keeps API identifiers unchanged when submitting a French form', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
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
    const fixture = TestBed.createComponent(IntegrationsPage);
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

  it('renders Microsoft Teams after Slack and saves a private webhook in French', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    expect(getMicrosoftTeamsConnection).toHaveBeenCalledWith(teamId);
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.microsoftTeamsConnectionError()).toBe(false);
    const text = fixture.nativeElement.textContent;
    expect(text.indexOf(teamEn.team.microsoftTeams)).toBeGreaterThan(
      text.indexOf(teamEn.team.slack),
    );
    page.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    const slackUrl = 'https://hooks.slack.com/services/T000/B000/test-token';
    page.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);

    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(teamFr.team.microsoftTeamsWebhookUrl);
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe(microsoftTeamsUrl);
    page.connectMicrosoftTeamsWebhook();
    fixture.detectChanges();

    expect(createMicrosoftTeamsConnection).toHaveBeenCalledWith(teamId, {
      webhookUrl: microsoftTeamsUrl,
    });
    expect(page.microsoftTeamsConnection()).toEqual(microsoftTeamsConnection);
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain(microsoftTeamsUrl);
    expect(page.slackWebhookForm.controls.webhookUrl.value).toBe(slackUrl);
    expect(page.slackConnection()).toBeNull();
  });

  it('loads, tests and removes a Microsoft Teams connection independently of Slack', async () => {
    getMicrosoftTeamsConnection.mockReturnValue(of(microsoftTeamsConnection));
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    expect(page.microsoftTeamsConnection()).toEqual(microsoftTeamsConnection);
    expect(page.microsoftTeamsConnectionLoading()).toBe(false);
    page.testMicrosoftTeamsWebhook();
    fixture.detectChanges();
    expect(testMicrosoftTeamsConnection).toHaveBeenCalledWith(teamId);
    expect(page.microsoftTeamsTestResult()?.success).toBe(true);
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.microsoftTeamsTestSuccess);
    expect(page.slackTestResult()).toBeNull();

    page.disconnectMicrosoftTeamsWebhook();
    expect(deleteMicrosoftTeamsConnection).toHaveBeenCalledWith(teamId);
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.microsoftTeamsTestResult()).toBeNull();
    expect(page.microsoftTeamsDisconnecting()).toBe(false);
  });

  it('shows load errors and retries Microsoft Teams without affecting Slack', async () => {
    getMicrosoftTeamsConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    expect(page.microsoftTeamsConnectionError()).toBe(true);
    expect(page.microsoftTeamsConnectionLoading()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.microsoftTeamsLoadError);
    expect(page.slackConnectionError()).toBe(false);
    getMicrosoftTeamsConnection.mockReturnValue(of(microsoftTeamsConnection));
    page.reloadMicrosoftTeamsConnection();
    expect(page.microsoftTeamsConnectionError()).toBe(false);
    expect(page.microsoftTeamsConnection()).toEqual(microsoftTeamsConnection);
  });

  it('preserves the form and clears pending state when saving fails', async () => {
    createMicrosoftTeamsConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    page.connectMicrosoftTeamsWebhook();
    expect(page.microsoftTeamsConnecting()).toBe(false);
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe(microsoftTeamsUrl);
  });

  it('handles unsuccessful tests and request failures without removing the connection', async () => {
    getMicrosoftTeamsConnection.mockReturnValue(of(microsoftTeamsConnection));
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    testMicrosoftTeamsConnection.mockReturnValue(
      of({ success: false, message: 'Delivery failed.' }),
    );
    page.testMicrosoftTeamsWebhook();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.microsoftTeamsTestFailure);
    expect(page.microsoftTeamsTesting()).toBe(false);

    testMicrosoftTeamsConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    page.testMicrosoftTeamsWebhook();
    expect(page.microsoftTeamsTesting()).toBe(false);
    expect(page.microsoftTeamsTestResult()).toBeNull();
    deleteMicrosoftTeamsConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    page.disconnectMicrosoftTeamsWebhook();
    expect(page.microsoftTeamsDisconnecting()).toBe(false);
    expect(page.microsoftTeamsConnection()).toEqual(microsoftTeamsConnection);
  });

  it('does not submit an invalid Microsoft Teams webhook', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(
      'https://example.com/webhook/test',
    );
    fixture.componentInstance.connectMicrosoftTeamsWebhook();
    expect(createMicrosoftTeamsConnection).not.toHaveBeenCalled();
  });

  it('allows signed Microsoft Teams URLs up to 4096 characters', async () => {
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const control = fixture.componentInstance.microsoftTeamsWebhookForm.controls.webhookUrl;
    const url = microsoftTeamsUrl + 'x'.repeat(4096 - microsoftTeamsUrl.length);
    control.setValue(url);
    expect(control.valid).toBe(true);
    control.setValue(url + 'x');
    expect(control.hasError('maxlength')).toBe(true);
    fixture.componentInstance.connectMicrosoftTeamsWebhook();
    expect(createMicrosoftTeamsConnection).not.toHaveBeenCalled();
  });

  it('resets Microsoft Teams state when changing teams and ignores stale responses', async () => {
    const response = new Subject<MicrosoftTeamsWebhookConnection>();
    getMicrosoftTeamsConnection.mockReturnValue(response);
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    expect(page.microsoftTeamsConnectionLoading()).toBe(true);
    page.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);

    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    response.next(microsoftTeamsConnection);
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.microsoftTeamsConnectionLoading()).toBe(false);
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
  });

  it.each(['success', 'error'])('cancels pending saves across A → B → A (%s)', async (outcome) => {
    const githubResponse = new Subject<GitHubConnection>();
    const slackResponse = new Subject<SlackWebhookConnection>();
    const microsoftResponse = new Subject<MicrosoftTeamsWebhookConnection>();
    TestBed.overrideProvider(GitHubApi, {
      useValue: { getConnection: getGitHubConnection, createConnection: () => githubResponse },
    });
    TestBed.overrideProvider(SlackApi, {
      useValue: { getConnection: getSlackConnection, createConnection: () => slackResponse },
    });
    createMicrosoftTeamsConnection.mockReturnValue(microsoftResponse);
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.openGitHubConnection();
    page.githubConnectionForm.setValue({
      owner: 'Example',
      ownerType: 'Organization',
      accessToken: 'private-test-token',
    });
    page.slackWebhookForm.controls.webhookUrl.setValue(
      'https://hooks.slack.com/services/T000/B000/private-test-token',
    );
    page.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    page.connectGitHub();
    page.connectSlackWebhook();
    page.connectMicrosoftTeamsWebhook();
    expect(page.githubConnecting()).toBe(true);
    const context = TestBed.inject(TeamContext);
    context.selectTeam('second-team');
    fixture.detectChanges();
    await fixture.whenStable();
    context.selectTeam(teamId);
    fixture.detectChanges();
    await fixture.whenStable();
    const success = vi.spyOn(TestBed.inject(NzMessageService), 'success');
    const error = vi.spyOn(TestBed.inject(NzMessageService), 'error');

    if (outcome === 'success') {
      githubResponse.next(githubConnection);
      slackResponse.next({ teamId, createdAt: '2026-09-01T00:00:00Z' });
      microsoftResponse.next(microsoftTeamsConnection);
    } else {
      githubResponse.error(new HttpErrorResponse({ status: 500 }));
      slackResponse.error(new HttpErrorResponse({ status: 500 }));
      microsoftResponse.error(new HttpErrorResponse({ status: 500 }));
    }
    expect(success).not.toHaveBeenCalled();
    expect(error).not.toHaveBeenCalled();
    expect(page.githubConnection()).toBeNull();
    expect(page.slackConnection()).toBeNull();
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.githubConnecting()).toBe(false);
    expect(page.slackConnecting()).toBe(false);
    expect(page.microsoftTeamsConnecting()).toBe(false);
    expect(page.githubConnectOpen()).toBe(false);
    expect(page.githubConnectionForm.controls.accessToken.value).toBe('');
    expect(page.slackWebhookForm.controls.webhookUrl.value).toBe('');
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
  });

  it('cancels connection fetches across A → B → A and clears prior results', async () => {
    const githubResponse = new Subject<GitHubConnection>();
    const slackResponse = new Subject<SlackWebhookConnection>();
    const microsoftResponse = new Subject<MicrosoftTeamsWebhookConnection>();
    getGitHubConnection.mockReturnValueOnce(githubResponse);
    getSlackConnection.mockReturnValueOnce(slackResponse);
    getMicrosoftTeamsConnection.mockReturnValueOnce(microsoftResponse);
    const fixture = TestBed.createComponent(IntegrationsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    page.githubTestResult.set({ success: true, organization: 'Example', message: 'Verified.' });
    page.githubSyncResult.set({ synchronized: 3, created: 2, updated: 1 });
    page.slackTestResult.set({ success: true, message: 'Sent.' });
    page.microsoftTeamsTestResult.set({ success: true, message: 'Sent.' });
    const context = TestBed.inject(TeamContext);
    context.selectTeam('second-team');
    fixture.detectChanges();
    await fixture.whenStable();
    context.selectTeam(teamId);
    fixture.detectChanges();
    await fixture.whenStable();

    githubResponse.next(githubConnection);
    slackResponse.next({ teamId, createdAt: '2026-09-01T00:00:00Z' });
    microsoftResponse.next(microsoftTeamsConnection);
    expect(githubResponse.observed).toBe(false);
    expect(slackResponse.observed).toBe(false);
    expect(microsoftResponse.observed).toBe(false);
    expect(page.githubConnection()).toBeNull();
    expect(page.slackConnection()).toBeNull();
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.githubTestResult()).toBeNull();
    expect(page.githubSyncResult()).toBeNull();
    expect(page.slackTestResult()).toBeNull();
    expect(page.microsoftTeamsTestResult()).toBeNull();
    expect(getGitHubConnection).toHaveBeenLastCalledWith(teamId);
    expect(getSlackConnection).toHaveBeenLastCalledWith(teamId);
    expect(getMicrosoftTeamsConnection).toHaveBeenLastCalledWith(teamId);
  });
});
