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
  TeamOutline,
  SyncOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { Observable, of, Subject, throwError } from 'rxjs';

import teamEn from '../../../../../../public/i18n/team/en.json';
import teamFr from '../../../../../../public/i18n/team/fr.json';
import { GitHubApi } from '../../services/github-api';
import { SlackApi } from '../../services/slack-api';
import { MicrosoftTeamsApi } from '../../services/microsoft-teams-api';
import { MicrosoftTeamsWebhookConnection } from '../../models/microsoft-teams-webhook-connection';
import { TestMicrosoftTeamsWebhookResponse } from '../../models/test-microsoft-teams-webhook-response';
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
  const microsoftTeamsConnection = { teamId, createdAt: '2026-09-01T00:00:00Z' };
  const microsoftTeamsUrl =
    'https://example.environment.api.powerplatform.com/powerautomate/automations/direct/workflows/workflow-id/triggers/manual/paths/invoke?sig=fake-test-signature';
  const getMicrosoftTeamsConnection = vi.fn<() => Observable<MicrosoftTeamsWebhookConnection>>();
  const createMicrosoftTeamsConnection = vi.fn<() => Observable<MicrosoftTeamsWebhookConnection>>();
  const testMicrosoftTeamsConnection = vi.fn<() => Observable<TestMicrosoftTeamsWebhookResponse>>();
  const deleteMicrosoftTeamsConnection = vi.fn<() => Observable<void>>();

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    createConnection.mockClear();
    createSlackConnection.mockClear();
    getMicrosoftTeamsConnection.mockReset().mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );
    createMicrosoftTeamsConnection.mockReset().mockReturnValue(of(microsoftTeamsConnection));
    testMicrosoftTeamsConnection.mockReset().mockReturnValue(
      of({ success: true, message: 'Notification sent.' }),
    );
    deleteMicrosoftTeamsConnection.mockReset().mockReturnValue(of(undefined));
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
          TeamOutline,
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
        {
          provide: MicrosoftTeamsApi,
          useValue: {
            getConnection: getMicrosoftTeamsConnection,
            createConnection: createMicrosoftTeamsConnection,
            testConnection: testMicrosoftTeamsConnection,
            deleteConnection: deleteMicrosoftTeamsConnection,
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

  it('renders Microsoft Teams after Slack and saves a private webhook in French', async () => {
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    expect(getMicrosoftTeamsConnection).toHaveBeenCalledWith(teamId);
    expect(page.microsoftTeamsConnection()).toBeNull();
    expect(page.microsoftTeamsConnectionError()).toBe(false);
    const text = fixture.nativeElement.textContent;
    expect(text.indexOf(teamEn.team.microsoftTeams)).toBeGreaterThan(text.indexOf(teamEn.team.slack));
    page.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    const slackUrl = 'https://hooks.slack.com/services/T000/B000/test-token';
    page.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);

    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(teamFr.team.microsoftTeamsWebhookUrl);
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe(microsoftTeamsUrl);
    page.connectMicrosoftTeamsWebhook();
    fixture.detectChanges();

    expect(createMicrosoftTeamsConnection).toHaveBeenCalledWith(teamId, { webhookUrl: microsoftTeamsUrl });
    expect(page.microsoftTeamsConnection()).toEqual(microsoftTeamsConnection);
    expect(page.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain(microsoftTeamsUrl);
    expect(page.slackWebhookForm.controls.webhookUrl.value).toBe(slackUrl);
    expect(page.slackConnection()).toBeNull();
  });

  it('loads, tests and removes a Microsoft Teams connection independently of Slack', async () => {
    getMicrosoftTeamsConnection.mockReturnValue(of(microsoftTeamsConnection));
    const fixture = TestBed.createComponent(TeamPage);
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
    const fixture = TestBed.createComponent(TeamPage);
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
    const fixture = TestBed.createComponent(TeamPage);
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
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const page = fixture.componentInstance;
    testMicrosoftTeamsConnection.mockReturnValue(of({ success: false, message: 'Delivery failed.' }));
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
    const fixture = TestBed.createComponent(TeamPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(
      'https://example.com/webhook/test',
    );
    fixture.componentInstance.connectMicrosoftTeamsWebhook();
    expect(createMicrosoftTeamsConnection).not.toHaveBeenCalled();
  });

  it('allows signed Microsoft Teams URLs up to 4096 characters', async () => {
    const fixture = TestBed.createComponent(TeamPage);
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
    const fixture = TestBed.createComponent(TeamPage);
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
});
