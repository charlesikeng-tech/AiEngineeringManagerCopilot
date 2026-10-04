import { TestBed } from '@angular/core/testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { Observable, of, Subject, throwError } from 'rxjs';
import {
  configureTeamTests,
  member,
  memberPage,
  teamId,
  teamWidgetFixture,
} from '../../testing/team-testing';
import { TeamMembers } from './team-members';

describe('team members', () => {
  let mocks: ReturnType<typeof configureTeamTests>;
  beforeEach(() => {
    mocks = configureTeamTests(TeamMembers);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('renders a compact identifying table with the server total, without unpaged loads', async () => {
    mocks.api.getMembersPage.mockReturnValue(
      of(memberPage([{ ...member, providerUserId: 'github-user' }])),
    );
    const fixture = await teamWidgetFixture(TeamMembers);
    expect(mocks.api.getMembersPage).toHaveBeenCalledWith(teamId, 1, 10);
    expect(mocks.api.getMembers).not.toHaveBeenCalled();
    expect(fixture.componentInstance.memberPagination.totalCount()).toBe(31);
    const table = fixture.nativeElement.querySelector('table.app-data-table');
    expect(table.textContent).toContain(member.email);
    expect(table.textContent).toContain('github-user');
    expect(table.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(table.querySelector('caption').textContent).toContain(
      TestBed.inject(I18nService).t('team.membersTitle'),
    );
  });

  it('requests the next page and resets to page one when size changes, ignoring duplicate changes', async () => {
    const widget = (await teamWidgetFixture(TeamMembers)).componentInstance;
    widget.changeMemberPage(2);
    expect(mocks.api.getMembersPage).toHaveBeenLastCalledWith(teamId, 2, 10);
    widget.changeMemberPageSize(20);
    expect(mocks.api.getMembersPage).toHaveBeenLastCalledWith(teamId, 1, 20);
    widget.changeMemberPageSize(20);
    expect(mocks.api.getMembersPage).toHaveBeenCalledTimes(3);
    expect(mocks.api.getMembers).not.toHaveBeenCalled();
  });

  it('cancels superseded requests and resets paging on selection change and clear', async () => {
    const cancelled = vi.fn();
    mocks.api.getMembersPage.mockReturnValueOnce(new Observable(() => cancelled));
    const fixture = await teamWidgetFixture(TeamMembers);
    const widget = fixture.componentInstance;
    const context = TestBed.inject(TeamContext);
    context.selectTeam('other-team');
    fixture.componentRef.setInput('teamId', 'other-team');
    fixture.componentRef.setInput('selectionVersion', context.selectionVersion());
    fixture.detectChanges();
    await fixture.whenStable();
    expect(cancelled).toHaveBeenCalledOnce();
    expect(mocks.api.getMembersPage).toHaveBeenLastCalledWith('other-team', 1, 10);
    widget.changeMemberPage(2);
    const clearCancelled = vi.fn();
    mocks.api.getMembersPage.mockReturnValueOnce(new Observable(() => clearCancelled));
    widget.loadMembers();
    context.clearTeam();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(clearCancelled).toHaveBeenCalledOnce();
    expect(widget.members()).toEqual([]);
    expect(widget.memberPagination.pageNumber()).toBe(1);
    expect(widget.memberPagination.totalCount()).toBe(0);
    const calls = mocks.api.getMembersPage.mock.calls.length;
    widget.loadMembers();
    expect(mocks.api.getMembersPage).toHaveBeenCalledTimes(calls);
  });

  it('cancels an older request when changing page and keeps paging after change detection', async () => {
    const fixture = await teamWidgetFixture(TeamMembers);
    const cancelled = vi.fn();
    mocks.api.getMembersPage.mockReturnValueOnce(new Observable(() => cancelled));
    fixture.componentInstance.loadMembers();
    fixture.componentInstance.changeMemberPage(2);
    expect(cancelled).toHaveBeenCalledOnce();
    expect(mocks.api.getMembersPage).toHaveBeenLastCalledWith(teamId, 2, 10);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.memberPagination.pageNumber()).toBe(2);
    expect(mocks.api.getMembersPage).toHaveBeenCalledTimes(3);
  });

  it('refreshes the current page after a created member, including trimmed provider ID', async () => {
    const widget = (await teamWidgetFixture(TeamMembers)).componentInstance;
    widget.changeMemberPage(2);
    widget.openAddMember();
    const editor = widget.editor()!;
    editor.addMemberForm.setValue({
      name: ' New member ',
      email: 'new@example.com',
      role: 'QA',
      providerUserId: ' provider-user ',
    });
    editor.saveMember();
    expect(mocks.api.createMember).toHaveBeenCalledWith(teamId, {
      name: 'New member',
      email: 'new@example.com',
      role: 'QA',
      providerUserId: 'provider-user',
    });
    expect(mocks.api.getMembersPage).toHaveBeenLastCalledWith(teamId, 2, 10);
    expect(mocks.api.getMembersPage).toHaveBeenCalledTimes(3);
    expect(editor.addMemberOpen()).toBe(false);
  });

  it('updates and removes members without reloading the team', async () => {
    const widget = (await teamWidgetFixture(TeamMembers)).componentInstance;
    widget.openEditMember(member);
    const editor = widget.editor()!;
    editor.addMemberForm.controls.name.setValue('Updated member');
    mocks.api.getMembersPage.mockReturnValueOnce(
      of(memberPage([{ ...member, name: 'Updated member' }])),
    );
    editor.saveMember();
    expect(mocks.api.updateMember).toHaveBeenCalledWith(teamId, member.id, {
      name: 'Updated member',
      email: member.email,
      role: 'Developer',
      providerUserId: null,
    });
    expect(widget.members()[0].name).toBe('Updated member');
    mocks.api.getMembersPage.mockReturnValueOnce(of(memberPage([], 0)));
    widget.deleteMember(member);
    expect(mocks.api.deleteMember).toHaveBeenCalledWith(teamId, member.id);
    expect(widget.members()).toEqual([]);
    expect(mocks.api.getMembersPage).toHaveBeenCalledTimes(3);
    expect(mocks.api.getTeam).not.toHaveBeenCalled();
  });

  it('clamps and refetches when deletion empties the last page', async () => {
    const widget = (await teamWidgetFixture(TeamMembers)).componentInstance;
    widget.changeMemberPage(2);
    mocks.api.getMembersPage.mockReturnValueOnce(of(memberPage([], 10)));
    widget.deleteMember(member);
    expect(mocks.api.getMembersPage.mock.calls.slice(-2)).toEqual([
      [teamId, 2, 10],
      [teamId, 1, 10],
    ]);
    expect(widget.memberPagination.pageNumber()).toBe(1);
    expect(widget.members()).toEqual([member]);
  });

  it('retries member page errors through the rendered button', async () => {
    mocks.api.getMembersPage.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    const fixture = await teamWidgetFixture(TeamMembers);
    expect(fixture.componentInstance.membersError()).toBe(true);
    const buttons: HTMLButtonElement[] = [...fixture.nativeElement.querySelectorAll('button')];
    const retry = buttons.find((button) =>
      button.textContent?.includes(TestBed.inject(I18nService).t('tables.retry')),
    )!;
    expect(retry).toBeDefined();
    retry.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.membersError()).toBe(false);
    expect(mocks.api.getTeam).not.toHaveBeenCalled();
    expect(mocks.message.error).toHaveBeenCalled();
  });

  it.each(['success', 'error'])(
    'rejects stale delete callbacks across same-tick A → B → A (%s)',
    async (outcome) => {
      const fixture = await teamWidgetFixture(TeamMembers);
      const widget = fixture.componentInstance;
      const pending = new Subject<void>();
      mocks.api.deleteMember.mockReturnValue(pending);
      widget.deleteMember(member);
      const context = TestBed.inject(TeamContext);
      context.selectTeam('another-team');
      context.selectTeam(teamId);
      const calls = mocks.api.getMembersPage.mock.calls.length;
      if (outcome === 'success') pending.next();
      else pending.error(new Error('unavailable'));
      expect(mocks.api.getMembersPage).toHaveBeenCalledTimes(calls);
      expect(mocks.message.success).not.toHaveBeenCalled();
      expect(mocks.message.error).not.toHaveBeenCalled();
      fixture.componentRef.setInput('selectionVersion', context.selectionVersion());
      fixture.detectChanges();
      await fixture.whenStable();
      expect(widget.deletingMemberId()).toBeNull();
      expect(widget.memberPagination.pageNumber()).toBe(1);
      expect(pending.observed).toBe(false);
    },
  );

  it('blocks stale or foreign members and keeps failed deletion retryable', async () => {
    const widget = (await teamWidgetFixture(TeamMembers)).componentInstance;
    widget.deleteMember({ ...member, teamId: 'other-team' });
    expect(mocks.api.deleteMember).not.toHaveBeenCalled();
    mocks.api.deleteMember.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    widget.deleteMember(member);
    expect(widget.deletingMemberId()).toBeNull();
    expect(widget.members()).toEqual([member]);
    expect(mocks.message.error).toHaveBeenCalled();
    TestBed.inject(TeamContext).selectTeam('other-team');
    widget.deleteMember(member);
    expect(mocks.api.deleteMember).toHaveBeenCalledTimes(1);
  });

  it('coordinates mutation buttons with the editor and list', async () => {
    const fixture = await teamWidgetFixture(TeamMembers);
    const widget = fixture.componentInstance;
    const pending = new Subject<typeof member>();
    mocks.api.updateMember.mockReturnValue(pending);
    const edit: HTMLButtonElement = fixture.nativeElement.querySelector('tbody button');
    edit.click();
    widget.editor()!.saveMember();
    fixture.detectChanges();
    expect(widget.memberMutationInProgress()).toBe(true);
    expect(fixture.nativeElement.querySelector('tbody button').disabled).toBe(true);
    widget.deleteMember(member);
    expect(mocks.api.deleteMember).not.toHaveBeenCalled();
    pending.next(member);
    expect(widget.memberMutationInProgress()).toBe(false);
  });
});
