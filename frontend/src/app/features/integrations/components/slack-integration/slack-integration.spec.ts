import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { of, Subject, throwError } from 'rxjs';
import {
  configureIntegrationTests,
  integrationFixture,
  slackUrl,
  teamFr,
  teamId,
  webhookConnection,
} from '../../testing/integration-testing';
import { SlackIntegration } from './slack-integration';

describe('Slack integration widget', () => {
  let mocks: ReturnType<typeof configureIntegrationTests>;
  beforeEach(() => {
    mocks = configureIntegrationTests(SlackIntegration);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('keeps the webhook private across language changes and clears it after saving', async () => {
    const fixture = await integrationFixture(SlackIntegration);
    const widget = fixture.componentInstance;
    widget.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(teamFr.team.slackWebhookUrl);
    expect(widget.slackWebhookForm.controls.webhookUrl.value).toBe(slackUrl);
    widget.connectSlackWebhook();
    fixture.detectChanges();
    expect(mocks.slack.createConnection).toHaveBeenCalledWith(teamId, { webhookUrl: slackUrl });
    expect(widget.slackWebhookForm.controls.webhookUrl.value).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain(slackUrl);
    expect(mocks.github.getConnection).not.toHaveBeenCalled();
  });

  it('loads, tests and disconnects independently', async () => {
    mocks.slack.getConnection.mockReturnValue(of(webhookConnection));
    const widget = (await integrationFixture(SlackIntegration)).componentInstance;
    widget.testSlackWebhook();
    expect(mocks.slack.testConnection).toHaveBeenCalledWith(teamId);
    expect(widget.slackTestResult()?.success).toBe(true);
    widget.disconnectSlackWebhook();
    expect(mocks.slack.deleteConnection).toHaveBeenCalledWith(teamId);
    expect(widget.slackConnection()).toBeNull();
    expect(widget.slackTestResult()).toBeNull();
  });

  it('retries loading errors while accepting absent connections', async () => {
    mocks.slack.getConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    const widget = (await integrationFixture(SlackIntegration)).componentInstance;
    expect(widget.slackConnectionError()).toBe(true);
    mocks.slack.getConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );
    widget.reloadSlackConnection();
    expect(widget.slackConnectionError()).toBe(false);
    expect(widget.slackConnectionLoading()).toBe(false);
    expect(widget.slackConnection()).toBeNull();
  });

  it.each([
    '',
    'http://hooks.slack.com/services/a/b/c',
    'https://example.com/services/a/b/c',
    'https://hooks.slack.com.evil.example/services/a/b/c',
    slackUrl + 'x'.repeat(2048),
  ])('rejects an invalid webhook (%s)', async (url) => {
    const widget = (await integrationFixture(SlackIntegration)).componentInstance;
    widget.slackWebhookForm.controls.webhookUrl.setValue(url);
    widget.connectSlackWebhook();
    expect(mocks.slack.createConnection).not.toHaveBeenCalled();
    expect(widget.slackWebhookForm.controls.webhookUrl.touched).toBe(true);
  });

  it('retains the form on save failure and handles test and removal failures', async () => {
    const widget = (await integrationFixture(SlackIntegration)).componentInstance;
    const failure = throwError(() => new Error('unavailable'));
    mocks.slack.createConnection.mockReturnValueOnce(failure);
    widget.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);
    widget.connectSlackWebhook();
    expect(widget.slackConnecting()).toBe(false);
    expect(widget.slackWebhookForm.controls.webhookUrl.value).toBe(slackUrl);
    widget.connectSlackWebhook();
    mocks.slack.testConnection.mockReturnValueOnce(of({ success: false, message: 'Failed.' }));
    widget.testSlackWebhook();
    expect(mocks.message.warning).toHaveBeenCalled();
    mocks.slack.testConnection.mockReturnValueOnce(failure);
    widget.testSlackWebhook();
    expect(widget.slackTesting()).toBe(false);
    expect(widget.slackTestResult()).toBeNull();
    mocks.slack.deleteConnection.mockReturnValueOnce(failure);
    widget.disconnectSlackWebhook();
    expect(widget.slackDisconnecting()).toBe(false);
    expect(widget.slackConnection()).toEqual(webhookConnection);
    expect(mocks.message.error).toHaveBeenCalledTimes(3);
  });

  it('cancels a pending independent widget save on team change', async () => {
    const fixture = await integrationFixture(SlackIntegration);
    const widget = fixture.componentInstance;
    const pending = new Subject<typeof webhookConnection>();
    mocks.slack.createConnection.mockReturnValue(pending);
    widget.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);
    widget.connectSlackWebhook();
    TestBed.inject(TeamContext).selectTeam('other-team');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(pending.observed).toBe(false);
    expect(widget.slackConnecting()).toBe(false);
    expect(widget.slackWebhookForm.controls.webhookUrl.value).toBe('');
    pending.next(webhookConnection);
    expect(widget.slackConnection()).toBeNull();
    expect(mocks.message.success).not.toHaveBeenCalled();
  });
});
