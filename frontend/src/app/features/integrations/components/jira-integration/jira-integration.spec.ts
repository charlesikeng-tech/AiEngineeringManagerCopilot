import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Observable, of, Subject, throwError } from 'rxjs';
import {
  JiraConnection,
  JiraSyncResult,
  TestJiraConnectionResponse,
} from '@features/integrations/providers/jira/models/jira-connection';
import { JiraApi } from '@features/integrations/providers/jira/data-access/jira-api';
import { JiraIntegration } from './jira-integration';

describe('JiraIntegration', () => {
  const teamId = 'team-a';
  const connection: JiraConnection = {
    id: 'jira-id',
    teamId,
    baseUrl: 'https://example.atlassian.net',
    email: 'manager@example.com',
    projectKey: 'EM',
    createdAt: '2026-10-01T00:00:00Z',
    lastSyncAt: null,
  };
  const getConnection = vi.fn<(id: string) => Observable<JiraConnection>>();
  const createConnection = vi.fn<() => Observable<JiraConnection>>();
  const testConnection = vi.fn<() => Observable<TestJiraConnectionResponse>>();
  const sync = vi.fn<() => Observable<JiraSyncResult>>();
  const deleteConnection = vi.fn<() => Observable<void>>();
  const message = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    getConnection
      .mockReset()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    createConnection.mockReset().mockReturnValue(of(connection));
    testConnection
      .mockReset()
      .mockReturnValue(of({ isValid: true, displayName: 'Manager', message: 'Valid' }));
    sync.mockReset().mockReturnValue(of({ total: 3, created: 1, updated: 2 }));
    deleteConnection.mockReset().mockReturnValue(of(undefined));
    Object.values(message).forEach((mock) => mock.mockClear());
    TestBed.configureTestingModule({
      imports: [JiraIntegration],
      providers: [
        ...provideI18nTesting(),
        { provide: NzMessageService, useValue: message },
        {
          provide: JiraApi,
          useValue: { getConnection, createConnection, testConnection, sync, deleteConnection },
        },
      ],
    });
    TestBed.inject(TeamContext).selectTeam(teamId);
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  async function render() {
    const fixture = TestBed.createComponent(JiraIntegration);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads the selected team and treats a missing connection as unconfigured', async () => {
    const fixture = await render();
    expect(getConnection).toHaveBeenCalledWith(teamId);
    expect(fixture.componentInstance.loadError()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Connect Jira');
  });

  it('does not request a connection when no team is selected', async () => {
    TestBed.inject(TeamContext).clearTeam();
    await render();
    expect(getConnection).not.toHaveBeenCalled();
  });

  it('creates the connection for the selected team and clears the secret', async () => {
    const fixture = await render();
    const component = fixture.componentInstance;
    component.openConnection();
    component.form.setValue({
      baseUrl: 'https://example.atlassian.net',
      email: 'manager@example.com',
      apiToken: 'fake-token',
      projectKey: 'em',
    });
    component.connect();
    fixture.detectChanges();
    expect(createConnection).toHaveBeenCalledWith(teamId, {
      baseUrl: connection.baseUrl,
      email: connection.email,
      apiToken: 'fake-token',
      projectKey: 'EM',
    });
    expect(component.form.controls.apiToken.value).toBe('');
    expect(component.connectOpen()).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('fake-token');
  });

  it('retains the form and surfaces a connection failure', async () => {
    createConnection.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    const fixture = await render();
    const component = fixture.componentInstance;
    component.openConnection();
    component.form.setValue({
      baseUrl: connection.baseUrl,
      email: connection.email,
      apiToken: 'fake-token',
      projectKey: 'EM',
    });
    component.connect();
    expect(component.saving()).toBe(false);
    expect(component.connectOpen()).toBe(true);
    expect(message.error).toHaveBeenCalledWith('Unable to save the Jira connection.');
  });

  it('rejects incomplete credentials without calling the API', async () => {
    const fixture = await render();
    fixture.componentInstance.connect();
    expect(createConnection).not.toHaveBeenCalled();
    expect(fixture.componentInstance.form.touched).toBe(true);
  });

  it.each([true, false])('shows the connection test outcome %s', async (isValid) => {
    getConnection.mockReturnValue(of(connection));
    testConnection.mockReturnValue(of({ isValid, displayName: null, message: 'Result' }));
    const fixture = await render();
    fixture.componentInstance.testConnection();
    expect(testConnection).toHaveBeenCalledWith(teamId);
    expect(fixture.componentInstance.testResult()?.isValid).toBe(isValid);
    expect(fixture.componentInstance.testing()).toBe(false);
  });

  it('synchronizes and reloads connection metadata', async () => {
    getConnection.mockReturnValue(of(connection));
    const fixture = await render();
    const lastSyncAt = '2026-10-03T13:00:00Z';
    getConnection.mockReturnValue(of({ ...connection, lastSyncAt }));
    fixture.componentInstance.sync();
    expect(sync).toHaveBeenCalledWith(teamId);
    expect(fixture.componentInstance.syncResult()).toEqual({ total: 3, created: 1, updated: 2 });
    expect(fixture.componentInstance.connection()?.lastSyncAt).toBe(lastSyncAt);
  });

  it('disconnects without changing the selected team', async () => {
    getConnection.mockReturnValue(of(connection));
    const fixture = await render();
    fixture.componentInstance.disconnect();
    expect(deleteConnection).toHaveBeenCalledWith(teamId);
    expect(fixture.componentInstance.connection()).toBeNull();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(teamId);
  });

  it.each(['test', 'sync', 'disconnect'] as const)(
    'surfaces a failed %s without losing the connection',
    async (operation) => {
      getConnection.mockReturnValue(of(connection));
      const failure = () => new HttpErrorResponse({ status: 503 });
      testConnection.mockReturnValue(throwError(failure));
      sync.mockReturnValue(throwError(failure));
      deleteConnection.mockReturnValue(throwError(failure));
      const fixture = await render();
      if (operation === 'test') {
        fixture.componentInstance.testConnection();
      } else if (operation === 'sync') {
        fixture.componentInstance.sync();
      } else {
        fixture.componentInstance.disconnect();
      }
      expect(message.error).toHaveBeenCalledOnce();
      expect(fixture.componentInstance.connection()).toEqual(connection);
      expect(fixture.componentInstance.busy()).toBe(false);
    },
  );

  it('shows load errors and allows retry', async () => {
    getConnection.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    const fixture = await render();
    expect(fixture.componentInstance.loadError()).toBe(true);
    getConnection.mockReturnValue(of(connection));
    fixture.componentInstance.reload();
    expect(fixture.componentInstance.loadError()).toBe(false);
    expect(fixture.componentInstance.connection()).toEqual(connection);
  });

  it('cancels old team requests and clears credentials and results when switching teams', async () => {
    getConnection.mockReturnValue(of(connection));
    const pending = new Subject<TestJiraConnectionResponse>();
    testConnection.mockReturnValue(pending);
    const fixture = await render();
    fixture.componentInstance.form.controls.apiToken.setValue('fake-token');
    fixture.componentInstance.testConnection();
    expect(pending.observed).toBe(true);

    getConnection.mockReturnValue(of({ ...connection, teamId: 'team-b', projectKey: 'B' }));
    TestBed.inject(TeamContext).selectTeam('team-b');
    fixture.detectChanges();
    await fixture.whenStable();
    pending.next({ isValid: true, displayName: 'Old team', message: 'Old result' });

    expect(pending.observed).toBe(false);
    expect(fixture.componentInstance.form.controls.apiToken.value).toBe('');
    expect(fixture.componentInstance.testResult()).toBeNull();
    expect(fixture.componentInstance.testing()).toBe(false);
    expect(fixture.componentInstance.connection()?.teamId).toBe('team-b');
  });

  it('preserves entered credentials when only the language changes', async () => {
    const fixture = await render();
    fixture.componentInstance.openConnection();
    fixture.componentInstance.form.controls.apiToken.setValue('fake-token');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Jeton API');
    expect(fixture.componentInstance.form.controls.apiToken.value).toBe('fake-token');
    expect(fixture.nativeElement.querySelector('#jira-api-token').type).toBe('password');
  });

  it('ignores an old request even after returning to the original team', async () => {
    const oldConnection = new Subject<JiraConnection>();
    getConnection.mockReturnValueOnce(oldConnection);
    const fixture = await render();

    getConnection.mockReturnValue(of({ ...connection, teamId: 'team-b' }));
    TestBed.inject(TeamContext).selectTeam('team-b');
    fixture.detectChanges();
    await fixture.whenStable();
    getConnection.mockReturnValue(of({ ...connection, projectKey: 'CURRENT' }));
    TestBed.inject(TeamContext).selectTeam(teamId);
    fixture.detectChanges();
    await fixture.whenStable();
    oldConnection.next({ ...connection, projectKey: 'OUTDATED' });

    expect(oldConnection.observed).toBe(false);
    expect(fixture.componentInstance.connection()?.projectKey).toBe('CURRENT');
  });

  it('rejects same-tick stale responses and clears secrets on a coalesced team switch', async () => {
    getConnection.mockReturnValue(of(connection));
    const pending = new Subject<TestJiraConnectionResponse>();
    testConnection.mockReturnValue(pending);
    const fixture = await render();
    fixture.componentInstance.form.controls.apiToken.setValue('fake-token');
    fixture.componentInstance.testConnection();
    TestBed.inject(TeamContext).selectTeam('team-b');
    TestBed.inject(TeamContext).selectTeam(teamId);
    pending.next({ isValid: true, displayName: 'Old result', message: 'Outdated' });
    expect(fixture.componentInstance.testResult()).toBeNull();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.form.controls.apiToken.value).toBe('');
    expect(fixture.componentInstance.testing()).toBe(false);
    expect(pending.observed).toBe(false);
  });
});
