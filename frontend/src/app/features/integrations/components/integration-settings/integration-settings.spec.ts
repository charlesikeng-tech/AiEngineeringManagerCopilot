import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { of, Subject } from 'rxjs';
import {
  configureIntegrationTests,
  commonEn,
  githubConnection,
  integrationFixture,
  microsoftTeamsUrl,
  slackUrl,
  teamEn,
  teamId,
  webhookConnection,
} from '../../testing/integration-testing';
import { GitHubIntegration } from '../github-integration/github-integration';
import { SlackIntegration } from '../slack-integration/slack-integration';
import { MicrosoftTeamsIntegration } from '../microsoft-teams-integration/microsoft-teams-integration';
import { IntegrationSettings } from './integration-settings';

describe('team-scoped integration composition', () => {
  let mocks: ReturnType<typeof configureIntegrationTests>;
  beforeEach(() => {
    mocks = configureIntegrationTests(IntegrationSettings);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('groups GitHub and Jira as sources and Slack before Teams as notifications', async () => {
    const fixture = await integrationFixture(IntegrationSettings);
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
    expect(notifications.textContent!.indexOf(teamEn.team.microsoftTeams)).toBeGreaterThan(
      notifications.textContent!.indexOf(teamEn.team.slack),
    );
  });

  it('does not fetch connections or show forms without a selected team', async () => {
    TestBed.overrideProvider(TeamApi, { useValue: { getTeams: () => of([]) } });
    const fixture = TestBed.createComponent(IntegrationSettings);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain(commonEn.integrations.noTeamDescription);
    expect(fixture.nativeElement.textContent).toContain(commonEn.integrations.noTeam);
    expect(fixture.nativeElement.querySelector('app-team-selector')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(mocks.github.getConnection).not.toHaveBeenCalled();
    expect(mocks.slack.getConnection).not.toHaveBeenCalled();
    expect(mocks.microsoftTeams.getConnection).not.toHaveBeenCalled();
  });

  it('disposes pending widgets and clears private form values when selection is cleared', async () => {
    const fixture = await integrationFixture(IntegrationSettings);
    const github = fixture.debugElement.query(By.directive(GitHubIntegration))
      .componentInstance as GitHubIntegration;
    const slack = fixture.debugElement.query(By.directive(SlackIntegration))
      .componentInstance as SlackIntegration;
    const teams = fixture.debugElement.query(By.directive(MicrosoftTeamsIntegration))
      .componentInstance as MicrosoftTeamsIntegration;
    const pending = new Subject<typeof webhookConnection>();
    mocks.microsoftTeams.createConnection.mockReturnValue(pending);
    github.openGitHubConnection();
    github.githubConnectionForm.controls.accessToken.setValue('private-test-token');
    slack.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);
    teams.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    teams.connectMicrosoftTeamsWebhook();
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('app-github-integration')).toBeNull();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(pending.observed).toBe(false);
    expect(github.githubConnectionForm.controls.accessToken.value).toBe('');
    expect(slack.slackWebhookForm.controls.webhookUrl.value).toBe('');
    expect(teams.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
    expect(teams.microsoftTeamsConnecting()).toBe(false);
  });

  it('keeps sibling forms and results independent while changing language and saving Teams', async () => {
    const fixture = await integrationFixture(IntegrationSettings);
    const slack = fixture.debugElement.query(By.directive(SlackIntegration))
      .componentInstance as SlackIntegration;
    const teams = fixture.debugElement.query(By.directive(MicrosoftTeamsIntegration))
      .componentInstance as MicrosoftTeamsIntegration;
    slack.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);
    teams.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    teams.connectMicrosoftTeamsWebhook();
    expect(slack.slackWebhookForm.controls.webhookUrl.value).toBe(slackUrl);
    expect(slack.slackConnection()).toBeNull();
    expect(slack.slackTestResult()).toBeNull();
    teams.testMicrosoftTeamsWebhook();
    expect(slack.slackTestResult()).toBeNull();
  });

  it.each(['success', 'error'])(
    'cancels all pending saves across A → B → A (%s)',
    async (outcome) => {
      const fixture = await integrationFixture(IntegrationSettings);
      const github = fixture.debugElement.query(By.directive(GitHubIntegration))
        .componentInstance as GitHubIntegration;
      const slack = fixture.debugElement.query(By.directive(SlackIntegration))
        .componentInstance as SlackIntegration;
      const teams = fixture.debugElement.query(By.directive(MicrosoftTeamsIntegration))
        .componentInstance as MicrosoftTeamsIntegration;
      const githubResponse = new Subject<typeof githubConnection>();
      const slackResponse = new Subject<typeof webhookConnection>();
      const teamsResponse = new Subject<typeof webhookConnection>();
      mocks.github.createConnection.mockReturnValue(githubResponse);
      mocks.slack.createConnection.mockReturnValue(slackResponse);
      mocks.microsoftTeams.createConnection.mockReturnValue(teamsResponse);
      github.openGitHubConnection();
      github.githubConnectionForm.setValue({
        owner: 'Example',
        ownerType: 'Organization',
        accessToken: 'private-test-token',
      });
      slack.slackWebhookForm.controls.webhookUrl.setValue(slackUrl);
      teams.microsoftTeamsWebhookForm.controls.webhookUrl.setValue(microsoftTeamsUrl);
      github.connectGitHub();
      slack.connectSlackWebhook();
      teams.connectMicrosoftTeamsWebhook();
      expect(github.githubConnecting()).toBe(true);
      const context = TestBed.inject(TeamContext);
      context.selectTeam('second-team');
      fixture.detectChanges();
      await fixture.whenStable();
      context.selectTeam(teamId);
      fixture.detectChanges();
      await fixture.whenStable();
      if (outcome === 'success') {
        githubResponse.next(githubConnection);
        slackResponse.next(webhookConnection);
        teamsResponse.next(webhookConnection);
      } else {
        githubResponse.error(new Error('unavailable'));
        slackResponse.error(new Error('unavailable'));
        teamsResponse.error(new Error('unavailable'));
      }
      expect(githubResponse.observed).toBe(false);
      expect(slackResponse.observed).toBe(false);
      expect(teamsResponse.observed).toBe(false);
      expect(mocks.message.success).not.toHaveBeenCalled();
      expect(mocks.message.error).not.toHaveBeenCalled();
      expect(github.githubConnection()).toBeNull();
      expect(slack.slackConnection()).toBeNull();
      expect(teams.microsoftTeamsConnection()).toBeNull();
      expect(github.githubConnecting()).toBe(false);
      expect(slack.slackConnecting()).toBe(false);
      expect(teams.microsoftTeamsConnecting()).toBe(false);
      expect(github.githubConnectOpen()).toBe(false);
      expect(github.githubConnectionForm.controls.accessToken.value).toBe('');
      expect(slack.slackWebhookForm.controls.webhookUrl.value).toBe('');
      expect(teams.microsoftTeamsWebhookForm.controls.webhookUrl.value).toBe('');
    },
  );

  it('cancels connection fetches across A → B → A and clears previous results', async () => {
    const githubResponse = new Subject<typeof githubConnection>();
    const slackResponse = new Subject<typeof webhookConnection>();
    const teamsResponse = new Subject<typeof webhookConnection>();
    mocks.github.getConnection.mockReturnValueOnce(githubResponse);
    mocks.slack.getConnection.mockReturnValueOnce(slackResponse);
    mocks.microsoftTeams.getConnection.mockReturnValueOnce(teamsResponse);
    const fixture = await integrationFixture(IntegrationSettings);
    const github = fixture.debugElement.query(By.directive(GitHubIntegration))
      .componentInstance as GitHubIntegration;
    const slack = fixture.debugElement.query(By.directive(SlackIntegration))
      .componentInstance as SlackIntegration;
    const teams = fixture.debugElement.query(By.directive(MicrosoftTeamsIntegration))
      .componentInstance as MicrosoftTeamsIntegration;
    github.githubTestResult.set({ success: true, organization: 'Example', message: 'Verified.' });
    github.githubSyncResult.set({ synchronized: 3, created: 2, updated: 1 });
    slack.slackTestResult.set({ success: true, message: 'Sent.' });
    teams.microsoftTeamsTestResult.set({ success: true, message: 'Sent.' });
    const context = TestBed.inject(TeamContext);
    context.selectTeam('second-team');
    fixture.detectChanges();
    await fixture.whenStable();
    context.selectTeam(teamId);
    fixture.detectChanges();
    await fixture.whenStable();
    githubResponse.next(githubConnection);
    slackResponse.next(webhookConnection);
    teamsResponse.next(webhookConnection);
    expect(githubResponse.observed).toBe(false);
    expect(slackResponse.observed).toBe(false);
    expect(teamsResponse.observed).toBe(false);
    expect(github.githubConnection()).toBeNull();
    expect(slack.slackConnection()).toBeNull();
    expect(teams.microsoftTeamsConnection()).toBeNull();
    expect(github.githubTestResult()).toBeNull();
    expect(github.githubSyncResult()).toBeNull();
    expect(slack.slackTestResult()).toBeNull();
    expect(teams.microsoftTeamsTestResult()).toBeNull();
    expect(mocks.github.getConnection).toHaveBeenLastCalledWith(teamId);
    expect(mocks.slack.getConnection).toHaveBeenLastCalledWith(teamId);
    expect(mocks.microsoftTeams.getConnection).toHaveBeenLastCalledWith(teamId);
  });
});
