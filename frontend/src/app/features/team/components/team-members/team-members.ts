import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TablePagination } from '@shared/ui/table-pagination/table-pagination';
import { PaginationState } from '@shared/pagination/pagination-state';
import { TeamContext } from '@core/team/team-context';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { TeamMember } from '@domains/teams/models/team-member';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { Subject, Subscription, takeUntil } from 'rxjs';
import { TeamMemberEditor } from '../team-member-editor/team-member-editor';

@Component({
  selector: 'app-team-members',
  imports: [
    TranslatePipe,
    LocalizedNumberPipe,
    TablePagination,
    TeamMemberEditor,
    NzAlertModule,
    NzButtonModule,
    NzEmptyModule,
    NzIconModule,
    NzPopconfirmModule,
    NzSpinModule,
  ],
  templateUrl: './team-members.html',
  styleUrl: './team-members.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamMembers {
  readonly teamId = input.required<string>();
  readonly selectionVersion = input.required<number>();
  readonly totalChanged = output<number>();
  readonly editor = viewChild(TeamMemberEditor);
  private readonly teamApi = inject(TeamApi);
  private readonly context = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
  private readonly changed = new Subject<void>();
  private memberLoadSubscription?: Subscription;
  readonly members = signal<readonly TeamMember[]>([]);
  readonly memberPagination = new PaginationState();
  readonly membersLoading = signal(false);
  readonly membersError = signal(false);
  readonly deletingMemberId = signal<string | null>(null);
  readonly memberMutationInProgress = () =>
    !!this.editor()?.creatingMember() || this.deletingMemberId() !== null;

  constructor() {
    effect(() => {
      this.teamId();
      this.selectionVersion();
      this.context.selectionVersion();
      untracked(() => {
        this.changed.next();
        this.memberLoadSubscription?.unsubscribe();
        this.memberPagination.reset();
        this.members.set([]);
        this.membersLoading.set(false);
        this.membersError.set(false);
        this.deletingMemberId.set(null);
        this.totalChanged.emit(0);
        if (this.current()) this.loadMembers();
      });
    });
  }

  loadMembers(): void {
    const teamId = this.teamId();
    const version = this.selectionVersion();
    if (!this.current(teamId, version)) return;
    this.memberLoadSubscription?.unsubscribe();
    this.membersLoading.set(true);
    this.membersError.set(false);
    const subscription = new Subscription();
    this.memberLoadSubscription = subscription;
    subscription.add(
      this.teamApi
        .getMembersPage(
          teamId,
          this.memberPagination.pageNumber(),
          this.memberPagination.pageSize(),
        )
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (page) => {
            if (!this.current(teamId, version)) return;
            if (this.memberPagination.acceptTotal(page.totalCount)) {
              this.totalChanged.emit(this.memberPagination.totalCount());
              this.loadMembers();
              return;
            }
            this.members.set(page.items);
            this.membersLoading.set(false);
            this.totalChanged.emit(this.memberPagination.totalCount());
          },
          error: (error) => {
            if (!this.current(teamId, version)) return;
            console.error('Failed to load team members', error);
            this.membersLoading.set(false);
            this.membersError.set(true);
            this.message.error(this.i18n.t('team.notifications.loadFailed'));
          },
        }),
    );
  }

  changeMemberPage(pageNumber: number): void {
    if (this.memberPagination.changePage(pageNumber)) this.loadMembers();
  }

  changeMemberPageSize(pageSize: number): void {
    if (this.memberPagination.changePageSize(pageSize)) this.loadMembers();
  }

  roleLabel(role: TeamMember['role']): string {
    return this.i18n.t(`team.roles.${role}`);
  }

  openAddMember(): void {
    if (this.current()) this.editor()?.openAddMember();
  }

  openEditMember(member: TeamMember): void {
    if (this.current()) this.editor()?.openEditMember(member);
  }

  deleteMember(member: TeamMember): void {
    const teamId = this.teamId();
    const version = this.selectionVersion();
    if (
      !this.current(teamId, version) ||
      member.teamId !== teamId ||
      this.memberMutationInProgress()
    )
      return;
    this.deletingMemberId.set(member.id);
    this.teamApi
      .deleteMember(teamId, member.id)
      .pipe(takeUntil(this.changed), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          if (!this.current(teamId, version)) return;
          this.loadMembers();
          this.deletingMemberId.set(null);
          this.message.success(this.i18n.t('team.notifications.memberRemoved'));
        },
        error: (error) => {
          if (!this.current(teamId, version)) return;
          console.error('Failed to remove team member', error);
          this.message.error(this.i18n.t('team.notifications.memberRemoveFailed'));
          this.deletingMemberId.set(null);
        },
      });
  }

  private current(teamId = this.teamId(), version = this.selectionVersion()): boolean {
    return (
      this.context.selectedTeamId() === teamId &&
      this.context.selectionVersion() === version &&
      this.teamId() === teamId &&
      this.selectionVersion() === version
    );
  }
}
