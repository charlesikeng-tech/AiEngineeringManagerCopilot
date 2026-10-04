import { OverlayContainer } from '@angular/cdk/overlay';
import { TestBed } from '@angular/core/testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { Subject, throwError } from 'rxjs';
import {
  configureTeamTests,
  member,
  teamFr,
  teamId,
  teamWidgetFixture,
} from '../../testing/team-testing';
import { TeamMemberEditor } from './team-member-editor';

describe('team member editor', () => {
  let mocks: ReturnType<typeof configureTeamTests>;
  beforeEach(() => {
    mocks = configureTeamTests(TeamMemberEditor);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('preserves member values when translating an open drawer', async () => {
    const fixture = await teamWidgetFixture(TeamMemberEditor);
    const editor = fixture.componentInstance;
    editor.openEditMember(member);
    editor.addMemberForm.controls.name.setValue('Updated member');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(editor.addMemberForm.controls.name.value).toBe('Updated member');
    expect(TestBed.inject(OverlayContainer).getContainerElement().textContent).toContain(
      teamFr.team.roles.Developer,
    );
    expect(editor.addMemberForm.controls.role.value).toBe('Developer');
  });

  it('validates required fields and limits and emits a saved event without loading pages', async () => {
    const fixture = await teamWidgetFixture(TeamMemberEditor);
    const editor = fixture.componentInstance;
    editor.openAddMember();
    editor.saveMember();
    expect(mocks.api.createMember).not.toHaveBeenCalled();
    expect(editor.addMemberForm.controls.email.touched).toBe(true);
    editor.addMemberForm.setValue({
      name: 'x'.repeat(201),
      email: 'invalid',
      role: 'QA',
      providerUserId: 'x'.repeat(201),
    });
    editor.saveMember();
    expect(mocks.api.createMember).not.toHaveBeenCalled();
    editor.addMemberForm.setValue({
      name: ' Name ',
      email: 'test@example.com',
      role: 'QA',
      providerUserId: ' ',
    });
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.saveMember();
    expect(mocks.api.createMember).toHaveBeenCalledWith(teamId, {
      name: 'Name',
      email: 'test@example.com',
      role: 'QA',
      providerUserId: null,
    });
    expect(saved).toHaveBeenCalledOnce();
    expect(mocks.api.getMembersPage).not.toHaveBeenCalled();
    expect(editor.addMemberForm.controls.name.value).toBe('');
    expect(editor.addMemberForm.controls.role.value).toBe('Developer');
  });

  it('blocks closing or saving twice while pending and retains values on failure', async () => {
    const fixture = await teamWidgetFixture(TeamMemberEditor);
    const editor = fixture.componentInstance;
    const pending = new Subject<typeof member>();
    mocks.api.updateMember.mockReturnValueOnce(pending);
    editor.openEditMember(member);
    editor.saveMember();
    editor.closeAddMember();
    editor.saveMember();
    expect(editor.addMemberOpen()).toBe(true);
    expect(mocks.api.updateMember).toHaveBeenCalledTimes(1);
    pending.error(new Error('unavailable'));
    expect(editor.creatingMember()).toBe(false);
    expect(editor.addMemberForm.controls.name.value).toBe(member.name);
    expect(editor.addMemberOpen()).toBe(true);
    expect(mocks.message.error).toHaveBeenCalled();
    editor.closeAddMember();
    expect(editor.editingMember()).toBeNull();
  });

  it.each(['create', 'update'] as const)(
    'preserves failed %s inputs and uses the corresponding notification',
    async (action) => {
      const editor = (await teamWidgetFixture(TeamMemberEditor)).componentInstance;
      if (action === 'create') {
        editor.openAddMember();
        editor.addMemberForm.setValue({
          name: member.name,
          email: member.email,
          role: member.role,
          providerUserId: '',
        });
        mocks.api.createMember.mockReturnValueOnce(throwError(() => new Error('unavailable')));
      } else {
        editor.openEditMember(member);
        mocks.api.updateMember.mockReturnValueOnce(throwError(() => new Error('unavailable')));
      }
      editor.saveMember();
      expect(editor.creatingMember()).toBe(false);
      expect(editor.addMemberForm.controls.name.value).toBe(member.name);
      expect(mocks.message.error).toHaveBeenCalledWith(
        TestBed.inject(I18nService).t(
          action === 'create'
            ? 'team.notifications.memberAddFailed'
            : 'team.notifications.memberUpdateFailed',
        ),
      );
    },
  );

  it.each(['create', 'update'] as const)(
    'rejects stale %s completions across same-tick A → B → A',
    async (action) => {
      const fixture = await teamWidgetFixture(TeamMemberEditor);
      const editor = fixture.componentInstance;
      const pending = new Subject<typeof member>();
      if (action === 'create') {
        mocks.api.createMember.mockReturnValueOnce(pending);
        editor.openAddMember();
        editor.addMemberForm.setValue({
          name: member.name,
          email: member.email,
          role: member.role,
          providerUserId: '',
        });
      } else {
        mocks.api.updateMember.mockReturnValueOnce(pending);
        editor.openEditMember(member);
      }
      const saved = vi.fn();
      editor.saved.subscribe(saved);
      editor.saveMember();
      const context = TestBed.inject(TeamContext);
      context.selectTeam('other-team');
      context.selectTeam(teamId);
      pending.next(member);
      expect(saved).not.toHaveBeenCalled();
      expect(mocks.message.success).not.toHaveBeenCalled();
      fixture.componentRef.setInput('selectionVersion', context.selectionVersion());
      fixture.detectChanges();
      await fixture.whenStable();
      expect(editor.creatingMember()).toBe(false);
      expect(editor.addMemberOpen()).toBe(false);
      expect(pending.observed).toBe(false);
    },
  );

  it('does not submit stale or foreign member forms to a newly selected team', async () => {
    const editor = (await teamWidgetFixture(TeamMemberEditor)).componentInstance;
    editor.openEditMember({ ...member, teamId: 'foreign-team' });
    expect(editor.addMemberOpen()).toBe(false);
    editor.openEditMember(member);
    TestBed.inject(TeamContext).selectTeam('other-team');
    editor.saveMember();
    expect(mocks.api.updateMember).not.toHaveBeenCalled();
  });
});
