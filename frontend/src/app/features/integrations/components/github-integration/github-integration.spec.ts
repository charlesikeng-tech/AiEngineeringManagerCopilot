import { OverlayContainer } from '@angular/cdk/overlay';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { of, Subject, throwError } from 'rxjs';
import {
  configureIntegrationTests,
  githubConnection,
  integrationFixture,
  teamEn,
  teamFr,
  teamId,
} from '../../testing/integration-testing';
import { GitHubIntegration } from './github-integration';

describe('GitHub integration widget', () => {
  let mocks: ReturnType<typeof configureIntegrationTests>;
  beforeEach(() => {
    mocks = configureIntegrationTests(GitHubIntegration);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('renders the drawer and translates labels without clearing entered values', async () => {
    const fixture = await integrationFixture(GitHubIntegration);
    const widget = fixture.componentInstance;
    widget.openGitHubConnection();
    fixture.detectChanges();
    await fixture.whenStable();
    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    expect(overlay.textContent).toContain(teamEn.team.githubAccount);
    expect(overlay.querySelector('input[formControlName="owner"]')).not.toBeNull();
    widget.githubConnectionForm.patchValue({
      owner: 'charlesikeng-tech',
      ownerType: 'Organization',
    });
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(overlay.textContent).toContain(teamFr.team.githubAccount);
    expect(widget.githubConnectionForm.controls.owner.value).toBe('charlesikeng-tech');
    expect(widget.githubConnectionForm.controls.ownerType.value).toBe('Organization');
  });

  it('keeps API identifiers unchanged in French and clears the submitted PAT', async () => {
    const fixture = await integrationFixture(GitHubIntegration);
    const widget = fixture.componentInstance;
    await TestBed.inject(I18nService).setLanguage('fr');
    widget.githubConnectionForm.setValue({
      owner: ' charlesikeng-tech ',
      ownerType: 'Organization',
      accessToken: ' fake-test-token ',
    });
    widget.connectGitHub();
    expect(mocks.github.createConnection).toHaveBeenCalledWith(teamId, {
      owner: 'charlesikeng-tech',
      ownerType: 'Organization',
      accessToken: 'fake-test-token',
    });
    expect(widget.githubConnectionForm.controls.accessToken.value).toBe('');
    expect(widget.githubConnectOpen()).toBe(false);
  });

  it('tests, synchronizes, refreshes server sync metadata and disconnects', async () => {
    mocks.github.getConnection.mockReturnValue(of(githubConnection));
    const fixture = await integrationFixture(GitHubIntegration);
    const widget = fixture.componentInstance;
    widget.testGitHubConnection();
    expect(mocks.github.testConnection).toHaveBeenCalledWith(teamId);
    expect(widget.githubTestResult()?.success).toBe(true);
    widget.syncGitHub();
    expect(mocks.github.sync).toHaveBeenCalledWith(teamId);
    expect(widget.githubSyncResult()).toEqual({ synchronized: 3, created: 2, updated: 1 });
    expect(mocks.github.getConnection).toHaveBeenCalledTimes(2);
    widget.disconnectGitHub();
    expect(mocks.github.deleteConnection).toHaveBeenCalledWith(teamId);
    expect(widget.githubConnection()).toBeNull();
    expect(widget.githubTestResult()).toBeNull();
    expect(widget.githubSyncResult()).toBeNull();
    expect(mocks.slack.getConnection).not.toHaveBeenCalled();
  });

  it('retries load errors and treats 404 as a normal disconnected state', async () => {
    mocks.github.getConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    const fixture = await integrationFixture(GitHubIntegration);
    const widget = fixture.componentInstance;
    expect(widget.githubConnectionError()).toBe(true);
    mocks.github.getConnection.mockReturnValue(of(githubConnection));
    widget.reloadGitHubConnection();
    expect(widget.githubConnectionError()).toBe(false);
    expect(widget.githubConnection()).toEqual(githubConnection);
    mocks.github.getConnection.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );
    widget.reloadGitHubConnection();
    expect(widget.githubConnection()).toBeNull();
    expect(widget.githubConnectionError()).toBe(false);
    expect(widget.githubConnectionLoading()).toBe(false);
  });

  it('validates blank credentials, blocks closing a pending drawer, and retains failed submissions', async () => {
    const fixture = await integrationFixture(GitHubIntegration);
    const widget = fixture.componentInstance;
    widget.openGitHubConnection();
    widget.connectGitHub();
    expect(mocks.github.createConnection).not.toHaveBeenCalled();
    widget.githubConnectionForm.setValue({ owner: ' ', ownerType: 'User', accessToken: ' ' });
    widget.connectGitHub();
    expect(mocks.github.createConnection).not.toHaveBeenCalled();
    const pending = new Subject<typeof githubConnection>();
    mocks.github.createConnection.mockReturnValue(pending);
    widget.githubConnectionForm.setValue({
      owner: 'Example',
      ownerType: 'User',
      accessToken: 'test-token',
    });
    widget.connectGitHub();
    widget.closeGitHubConnection();
    expect(widget.githubConnectOpen()).toBe(true);
    pending.error(new Error('unavailable'));
    expect(widget.githubConnecting()).toBe(false);
    expect(widget.githubConnectionForm.controls.accessToken.value).toBe('test-token');
    expect(mocks.message.error).toHaveBeenCalled();
    widget.closeGitHubConnection();
    expect(widget.githubConnectionForm.controls.accessToken.value).toBe('');
  });

  it.each(['test', 'sync', 'delete'] as const)(
    'displays %s errors without discarding a connection',
    async (action) => {
      mocks.github.getConnection.mockReturnValue(of(githubConnection));
      const widget = (await integrationFixture(GitHubIntegration)).componentInstance;
      const failure = throwError(() => new Error('unavailable'));
      if (action === 'test') {
        mocks.github.testConnection.mockReturnValue(failure);
        widget.testGitHubConnection();
        expect(widget.githubTesting()).toBe(false);
      } else if (action === 'sync') {
        mocks.github.sync.mockReturnValue(failure);
        widget.syncGitHub();
        expect(widget.githubSyncing()).toBe(false);
      } else {
        mocks.github.deleteConnection.mockReturnValue(failure);
        widget.disconnectGitHub();
        expect(widget.githubDisconnecting()).toBe(false);
      }
      expect(widget.githubConnection()).toEqual(githubConnection);
      expect(mocks.message.error).toHaveBeenCalled();
    },
  );

  it('rejects same-tick stale sync results without refreshing or notifying', async () => {
    mocks.github.getConnection.mockReturnValue(of(githubConnection));
    const widget = (await integrationFixture(GitHubIntegration)).componentInstance;
    const pending = new Subject<{ synchronized: number; created: number; updated: number }>();
    mocks.github.sync.mockReturnValue(pending);
    widget.syncGitHub();
    const context = TestBed.inject(TeamContext);
    context.selectTeam('other-team');
    context.selectTeam(teamId);
    pending.next({ synchronized: 1, created: 1, updated: 0 });
    expect(widget.githubSyncResult()).toBeNull();
    expect(mocks.github.getConnection).toHaveBeenCalledTimes(1);
    expect(mocks.message.success).not.toHaveBeenCalled();
  });
});
