import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { of, Subject, throwError } from 'rxjs';
import {
  configureIntegrationTests,
  integrationFixture,
  microsoftTeamsUrl,
  teamEn,
  teamFr,
  teamId,
  webhookConnection,
} from '../../testing/integration-testing';
import { MicrosoftTeamsIntegration } from './microsoft-teams-integration';

describe('Microsoft Teams integration widget', () => {
  let mocks: ReturnType<typeof configureIntegrationTests>;
  beforeEach(() => {
    mocks = configureIntegrationTests(MicrosoftTeamsIntegration);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('loads a missing connection normally and saves a private webhook in French', async () => {
    const fixture = await integrationFixture(MicrosoftTeamsIntegration);
    const widget = fixture.componentInstance;
    expect(mocks.microsoftTeams.getConnection).toHaveBeenCalledWith(teamId);
    expect(widget.microsoftTeamsConnection()).toBeNull();
    expect(widget.microsoftTeamsConnectionError()).toBe(false);
    widget.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(teamFr.team.microsoftTeamsWebhookUrl);
    expect(widget.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe(microsoftTeamsUrl);
    widget.connectMicrosoftTeamsWebhook();
    fixture.detectChanges();
    expect(mocks.microsoftTeams.createConnection).toHaveBeenCalledWith(teamId, {
      webhookUrl: microsoftTeamsUrl,
    });
    expect(widget.microsoftTeamsConnection()).toEqual(webhookConnection);
    expect(widget.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain(microsoftTeamsUrl);
    expect(mocks.slack.getConnection).not.toHaveBeenCalled();
  });

  it('loads, tests and removes its connection', async () => {
    mocks.microsoftTeams.getConnection.mockReturnValue(of(webhookConnection));
    const fixture = await integrationFixture(MicrosoftTeamsIntegration);
    const widget = fixture.componentInstance;
    expect(widget.microsoftTeamsConnection()).toEqual(webhookConnection);
    expect(widget.microsoftTeamsConnectionLoading()).toBe(false);
    widget.testMicrosoftTeamsWebhook();
    fixture.detectChanges();
    expect(mocks.microsoftTeams.testConnection).toHaveBeenCalledWith(teamId);
    expect(widget.microsoftTeamsTestResult()?.success).toBe(true);
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.microsoftTeamsTestSuccess);
    widget.disconnectMicrosoftTeamsWebhook();
    expect(mocks.microsoftTeams.deleteConnection).toHaveBeenCalledWith(teamId);
    expect(widget.microsoftTeamsConnection()).toBeNull();
    expect(widget.microsoftTeamsTestResult()).toBeNull();
    expect(widget.microsoftTeamsDisconnecting()).toBe(false);
  });

  it('shows load errors and retries', async () => {
    mocks.microsoftTeams.getConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    const fixture = await integrationFixture(MicrosoftTeamsIntegration);
    const widget = fixture.componentInstance;
    expect(widget.microsoftTeamsConnectionError()).toBe(true);
    expect(widget.microsoftTeamsConnectionLoading()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.microsoftTeamsLoadError);
    mocks.microsoftTeams.getConnection.mockReturnValue(of(webhookConnection));
    widget.reloadMicrosoftTeamsConnection();
    expect(widget.microsoftTeamsConnectionError()).toBe(false);
    expect(widget.microsoftTeamsConnection()).toEqual(webhookConnection);
  });

  it('preserves the form and clears pending state on save failure', async () => {
    mocks.microsoftTeams.createConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    const widget = (await integrationFixture(MicrosoftTeamsIntegration)).componentInstance;
    widget.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    widget.connectMicrosoftTeamsWebhook();
    expect(widget.microsoftTeamsConnecting()).toBe(false);
    expect(widget.microsoftTeamsConnection()).toBeNull();
    expect(widget.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe(microsoftTeamsUrl);
    expect(mocks.message.error).toHaveBeenCalled();
  });

  it('handles unsuccessful tests and request failures without removing its connection', async () => {
    mocks.microsoftTeams.getConnection.mockReturnValue(of(webhookConnection));
    const fixture = await integrationFixture(MicrosoftTeamsIntegration);
    const widget = fixture.componentInstance;
    mocks.microsoftTeams.testConnection.mockReturnValue(
      of({ success: false, message: 'Delivery failed.' }),
    );
    widget.testMicrosoftTeamsWebhook();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(teamEn.team.microsoftTeamsTestFailure);
    expect(widget.microsoftTeamsTesting()).toBe(false);
    expect(mocks.message.warning).toHaveBeenCalled();
    mocks.microsoftTeams.testConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    widget.testMicrosoftTeamsWebhook();
    expect(widget.microsoftTeamsTesting()).toBe(false);
    expect(widget.microsoftTeamsTestResult()).toBeNull();
    mocks.microsoftTeams.deleteConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    widget.disconnectMicrosoftTeamsWebhook();
    expect(widget.microsoftTeamsDisconnecting()).toBe(false);
    expect(widget.microsoftTeamsConnection()).toEqual(webhookConnection);
    expect(mocks.message.error).toHaveBeenCalledTimes(2);
  });

  it.each(['https://example.com/webhook/test', 'http://example.webhook.office.com/test'])(
    'does not submit invalid webhooks (%s)',
    async (url) => {
      const widget = (await integrationFixture(MicrosoftTeamsIntegration)).componentInstance;
      widget.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(url);
      widget.connectMicrosoftTeamsWebhook();
      expect(mocks.microsoftTeams.createConnection).not.toHaveBeenCalled();
    },
  );

  it('allows signed URLs up to 4096 characters', async () => {
    const widget = (await integrationFixture(MicrosoftTeamsIntegration)).componentInstance;
    const control = widget.microsoftTeamsWebhookForm.controls.webhookUrl;
    const url = microsoftTeamsUrl + 'x'.repeat(4096 - microsoftTeamsUrl.length);
    control.setValue(url);
    expect(control.valid).toBe(true);
    control.setValue(url + 'x');
    expect(control.hasError('maxlength')).toBe(true);
    widget.connectMicrosoftTeamsWebhook();
    expect(mocks.microsoftTeams.createConnection).not.toHaveBeenCalled();
  });

  it('resets state and cancels connection loading when the team is cleared', async () => {
    const response = new Subject<typeof webhookConnection>();
    mocks.microsoftTeams.getConnection.mockReturnValue(response);
    const fixture = await integrationFixture(MicrosoftTeamsIntegration);
    const widget = fixture.componentInstance;
    expect(widget.microsoftTeamsConnectionLoading()).toBe(true);
    widget.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    response.next(webhookConnection);
    expect(response.observed).toBe(false);
    expect(widget.microsoftTeamsConnection()).toBeNull();
    expect(widget.microsoftTeamsConnectionLoading()).toBe(false);
    expect(widget.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
  });

  it('rejects same-tick stale tests across A → B → A and resets pending state', async () => {
    mocks.microsoftTeams.getConnection.mockReturnValue(of(webhookConnection));
    const fixture = await integrationFixture(MicrosoftTeamsIntegration);
    const widget = fixture.componentInstance;
    const pending = new Subject<{ success: boolean; message: string }>();
    mocks.microsoftTeams.testConnection.mockReturnValue(pending);
    widget.testMicrosoftTeamsWebhook();
    const context = TestBed.inject(TeamContext);
    context.selectTeam('other-team');
    context.selectTeam(teamId);
    pending.next({ success: true, message: 'Outdated' });
    expect(widget.microsoftTeamsTestResult()).toBeNull();
    expect(mocks.message.success).not.toHaveBeenCalled();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(widget.microsoftTeamsTesting()).toBe(false);
    expect(pending.observed).toBe(false);
  });
});
