import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import type { EngineeringAction } from '@domains/engineering/models/action';
import managementEn from '../../../../../../public/i18n/management/en.json';
import managementFr from '../../../../../../public/i18n/management/fr.json';
import { PagedActionsResponse } from '../../models/paged-actions-response';
import { ActionsApi } from '../../services/actions-api';
import { Actions } from './actions';

describe('Actions server pagination', () => {
  const action: EngineeringAction = {
    id: 'action-1', reportId: 'report-1', metricType: 'CycleTime', priority: 'High',
    title: 'Improve delivery', description: 'Detailed action evidence', owner: 'Ada',
    dueDate: '2026-09-29', status: 'Todo', createdAt: '2026-10-01',
  };
  const getCurrentActions = vi.fn();
  const getActionsPage = vi.fn<(...args: [string, number, number]) => Observable<PagedActionsResponse>>();
  const updateAction = vi.fn<(...args: unknown[]) => Observable<EngineeringAction>>();
  const message = { success: vi.fn(), error: vi.fn() };
  const response = (items = [action], totalCount = 25): PagedActionsResponse => ({
    teamId: 'team-a', reportId: 'report-1', periodStart: '2026-09-01', periodEnd: '2026-09-30',
    page: { items, totalCount, pageNumber: 1, pageSize: 10 },
    summary: { todoCount: 10, inProgressCount: 8, doneCount: 4, cancelledCount: 3, overdueCount: 6, asOfDate: '2026-09-30' },
  });

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    vi.clearAllMocks();
    getActionsPage.mockReset().mockReturnValue(of(response()));
    updateAction.mockReset().mockReturnValue(of({ ...action, status: 'Done' }));
    TestBed.configureTestingModule({
      imports: [Actions],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: managementEn, fr: managementFr }),
        { provide: ActionsApi, useValue: { getCurrentActions, getActionsPage, updateAction } },
        { provide: NzMessageService, useValue: message },
      ],
    });
    TestBed.inject(TeamContext).selectTeam('team-a');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  async function render() {
    const fixture = TestBed.createComponent(Actions);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  it('fetches only the default page, preserves all edit controls and uses global summaries', async () => {
    const fixture = await render();
    const page = fixture.componentInstance;
    expect(getActionsPage).toHaveBeenCalledExactlyOnceWith('team-a', 1, 10);
    expect(getCurrentActions).not.toHaveBeenCalled();
    expect(page.totalCount()).toBe(25);
    expect([page.todoCount(), page.inProgressCount(), page.doneCount(), page.cancelledCount(), page.overdueCount()])
      .toEqual([10, 8, 4, 3, 6]);
    expect(fixture.nativeElement.querySelectorAll('.action-card')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('#owner-action-1')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.status-select')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('nz-date-picker')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain(action.description);
    expect(fixture.nativeElement.textContent).toContain('of 25 items');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Éléments par page');
    expect(fixture.nativeElement.textContent).toContain('sur 25 éléments');
  });

  it('loads next pages lazily, keeps server order and resets the page on size changes', async () => {
    const page = (await render()).componentInstance;
    const low: EngineeringAction = { ...action, id: 'low', priority: 'Low' };
    getActionsPage.mockReturnValueOnce(of(response([low, action])));
    page.changePage(2);
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 2, 10);
    expect(page.sortedActions().map((item) => item.id)).toEqual(['low', 'action-1']);
    page.changePageSize(20);
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 1, 20);
    page.changePageSize(20);
    expect(getActionsPage).toHaveBeenCalledTimes(3);
  });

  it('uses the server UTC date for due-state labels, independently of local time', async () => {
    const page = (await render()).componentInstance;
    const view = page.actions()[0];
    expect(page.dueState(view)).toBe('overdue');
    expect(page.dueState({ ...view, dueDate: '2026-09-30' })).toBe('soon');
    expect(page.dueState({ ...view, dueDate: '2026-10-03' })).toBe('soon');
    expect(page.dueState({ ...view, dueDate: '2026-10-04' })).toBe('scheduled');
    expect(page.dueState({ ...view, status: 'Done' })).toBeNull();
    expect(page.dueState({ ...view, status: 'Cancelled' })).toBeNull();
    expect(page.overdueCount()).toBe(6);
  });

  it('reloads the current page after status updates to refresh server ordering and global counts', async () => {
    const page = (await render()).componentInstance;
    page.changePage(2);
    const updated = { ...action, status: 'Done' as const };
    const refreshed = response([updated]);
    refreshed.summary.doneCount = 5;
    refreshed.summary.todoCount = 9;
    getActionsPage.mockReturnValueOnce(of(refreshed));
    page.updateStatus(page.actions()[0], 'Done');
    expect(updateAction).toHaveBeenCalledExactlyOnceWith('team-a', 'action-1', { status: 'Done' });
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 2, 10);
    expect(page.pagination.pageNumber()).toBe(2);
    expect(page.actions()[0].status).toBe('Done');
    expect(page.doneCount()).toBe(5);
    expect(page.todoCount()).toBe(9);
    expect(message.success).toHaveBeenCalledOnce();
  });

  it('retains owner and due-date editing and reloads without resetting the selected page', async () => {
    const page = (await render()).componentInstance;
    page.changePage(2);
    updateAction.mockReturnValueOnce(of({ ...action, owner: 'Grace' }));
    page.updateOwner(action, ' Grace ');
    expect(updateAction).toHaveBeenLastCalledWith('team-a', 'action-1', { owner: 'Grace' });
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 2, 10);
    updateAction.mockReturnValueOnce(of({ ...action, dueDate: '2026-10-05' }));
    page.updateDueDate(action, new Date(2026, 9, 5));
    expect(updateAction).toHaveBeenLastCalledWith('team-a', 'action-1', { dueDate: '2026-10-05' });
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 2, 10);
  });

  it('clamps and refetches if the action update removes the last page', async () => {
    const page = (await render()).componentInstance;
    page.changePage(3);
    getActionsPage.mockReturnValueOnce(of(response([], 10)));
    page.updateStatus(action, 'Done');
    expect(getActionsPage.mock.calls.slice(-2)).toEqual([['team-a', 3, 10], ['team-a', 1, 10]]);
    expect(page.pagination.pageNumber()).toBe(1);
    expect(page.actions()).toHaveLength(1);
  });

  it('keeps existing values when an edit fails and permits subsequent edits', async () => {
    const page = (await render()).componentInstance;
    vi.spyOn(console, 'error').mockImplementation(() => {});
    updateAction.mockReturnValueOnce(throwError(() => new Error('failed')));
    page.updateStatus(action, 'Done');
    expect(page.updatingActionId()).toBeNull();
    expect(page.actions()[0].status).toBe('Todo');
    expect(getActionsPage).toHaveBeenCalledTimes(1);
    expect(message.error).toHaveBeenCalledOnce();
    page.updateStatus(action, 'Done');
    expect(getActionsPage).toHaveBeenCalledTimes(2);
  });

  it('survives load errors with retry and a later team switch', async () => {
    getActionsPage.mockReturnValueOnce(throwError(() => ({ status: 500 })));
    const fixture = await render();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Retry');
    button.click();
    expect(fixture.componentInstance.actions()).toHaveLength(1);
    TestBed.inject(TeamContext).selectTeam('team-b');
    fixture.detectChanges();
    expect(getActionsPage).toHaveBeenLastCalledWith('team-b', 1, 10);
  });

  it('treats no-report 404 as empty but does not hide unauthorized errors', async () => {
    getActionsPage.mockReturnValueOnce(throwError(() => ({ status: 401 })));
    const page = (await render()).componentInstance;
    expect(page.error()).toBe(true);
    getActionsPage.mockReturnValueOnce(throwError(() => ({ status: 404 })));
    page.retry();
    expect(page.error()).toBe(false);
    expect(page.data()).toBeNull();
  });

  it('cancels in-flight page loads and mutations when team selection is cleared', async () => {
    const fixture = await render();
    const cancelUpdate = vi.fn();
    const cancelLoad = vi.fn();
    updateAction.mockReturnValueOnce(new Observable(() => cancelUpdate));
    fixture.componentInstance.updateStatus(action, 'Done');
    getActionsPage.mockReturnValueOnce(new Observable(() => cancelLoad));
    fixture.componentInstance.retry();
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    expect(cancelUpdate).toHaveBeenCalledOnce();
    expect(cancelLoad).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.updatingActionId()).toBeNull();
    expect(fixture.componentInstance.loading()).toBe(false);
    expect(fixture.componentInstance.data()).toBeNull();
  });

  it('rejects mutation success synchronously across A→B→A, without toast or reload', async () => {
    const fixture = await render();
    const pending = new Subject<EngineeringAction>();
    updateAction.mockReturnValueOnce(pending);
    fixture.componentInstance.updateStatus(action, 'Done');
    const team = TestBed.inject(TeamContext);
    team.selectTeam('team-b');
    team.selectTeam('team-a');
    pending.next({ ...action, status: 'Done' });
    expect(message.success).not.toHaveBeenCalled();
    expect(getActionsPage).toHaveBeenCalledTimes(1);
    fixture.detectChanges();
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 1, 10);
    expect(fixture.componentInstance.actions()[0].status).toBe('Todo');
  });

  it('rejects stale page and mutation errors before selection effects run', async () => {
    const fixture = await render();
    const load = new Subject<PagedActionsResponse>();
    const mutation = new Subject<EngineeringAction>();
    updateAction.mockReturnValueOnce(mutation);
    fixture.componentInstance.updateStatus(action, 'Done');
    getActionsPage.mockReturnValueOnce(load);
    fixture.componentInstance.changePage(2);
    TestBed.inject(TeamContext).selectTeam('team-b');
    load.error({ status: 500 });
    mutation.error(new Error('old update failed'));
    expect(fixture.componentInstance.error()).toBe(false);
    expect(message.error).not.toHaveBeenCalled();
    fixture.detectChanges();
    expect(getActionsPage).toHaveBeenLastCalledWith('team-b', 1, 10);
  });

  it('rejects a stale load result even if selection returns to the original team', async () => {
    const fixture = await render();
    const pending = new Subject<PagedActionsResponse>();
    getActionsPage.mockReturnValueOnce(pending);
    fixture.componentInstance.changePage(2);
    const team = TestBed.inject(TeamContext);
    team.selectTeam('team-b');
    team.selectTeam('team-a');
    pending.next(response([{ ...action, id: 'obsolete' }], 99));
    expect(fixture.componentInstance.actions().some((item) => item.id === 'obsolete')).toBe(false);
    expect(fixture.componentInstance.totalCount()).toBe(25);
    fixture.detectChanges();
    expect(getActionsPage).toHaveBeenLastCalledWith('team-a', 1, 10);
  });

  it('does not submit an edit from stale rendered cards under a new team', async () => {
    const fixture = await render();
    TestBed.inject(TeamContext).selectTeam('team-b');
    fixture.componentInstance.updateStatus(action, 'Done');
    expect(updateAction).not.toHaveBeenCalled();
  });
});
