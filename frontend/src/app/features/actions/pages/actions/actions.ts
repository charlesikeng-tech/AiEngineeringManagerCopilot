import { UpperCasePipe } from '@angular/common';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { PaginationState } from '@core/models/pagination-state';
import { TablePagination } from '@core/components/table-pagination/table-pagination';
import { PagedActionsResponse } from '../../models/paged-actions-response';

import { TeamContext } from '@core/team/team-context';
import {
  ActionStatus,
  EngineeringAction,
  UpdateEngineeringActionRequest,
} from '@features/dashboard/models/engineering-dashboard-response';
import { ActionsApi } from '../../services/actions-api';

type EngineeringActionView = EngineeringAction & {
  dueDateValue: Date | null;
};

@Component({
  selector: 'app-actions',
  standalone: true,
  imports: [
    TablePagination,
    FormsModule,
    RouterLink,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    TranslatePipe,
    UpperCasePipe,
    NzAlertModule,
    NzDatePickerModule,
    NzEmptyModule,
    NzInputModule,
    NzSelectModule,
    NzSpinModule,
  ],
  templateUrl: './actions.html',
  styleUrl: './actions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Actions {
  readonly i18n = inject(I18nService);
  private readonly actionsApi = inject(ActionsApi);
  private readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);

  readonly loading = signal(false);
  readonly error = signal(false);
  readonly updatingActionId = signal<string | null>(null);

  readonly data = signal<PagedActionsResponse | null>(null);
  readonly actions = signal<readonly EngineeringActionView[]>([]);
  readonly pagination = new PaginationState();
  private readonly loadActions$ = new Subject<{ teamId: string; version: number } | null>();
  private readonly cancelMutations$ = new Subject<void>();
  private loadedSelectionVersion: number | null = null;

  readonly todoCount = computed(() => this.data()?.summary.todoCount ?? 0);

  readonly inProgressCount = computed(() => this.data()?.summary.inProgressCount ?? 0);

  readonly doneCount = computed(() => this.data()?.summary.doneCount ?? 0);

  readonly cancelledCount = computed(() => this.data()?.summary.cancelledCount ?? 0);

  readonly totalCount = this.pagination.totalCount;
  readonly sortedActions = this.actions;

  readonly overdueCount = computed(() => this.data()?.summary.overdueCount ?? 0);

  constructor() {
    this.loadActions$
      .pipe(
        switchMap((request) => {
          if (!request) return EMPTY;
          this.error.set(false);
          this.data.set(null);
          this.actions.set([]);
          this.loadedSelectionVersion = null;
          this.loading.set(true);

          return this.actionsApi
            .getActionsPage(
              request.teamId,
              this.pagination.pageNumber(),
              this.pagination.pageSize(),
            )
            .pipe(
              switchMap((response) => {
                if (request.version !== this.teamContext.selectionVersion()) return EMPTY;
                if (this.pagination.acceptTotal(response.page.totalCount)) {
                  this.retry();
                  return EMPTY;
                }
                this.data.set(response);
                this.loadedSelectionVersion = request.version;
                this.actions.set(response.page.items.map((action) => this.toActionView(action)));
                this.loading.set(false);
                return EMPTY;
              }),
              catchError((error) => {
                if (request.version === this.teamContext.selectionVersion()) {
                  this.data.set(null);
                  this.actions.set([]);
                  if (error.status === 404) this.pagination.acceptTotal(0);
                  this.error.set(error.status !== 404);
                  this.loading.set(false);
                }
                return EMPTY;
              }),
            );
        }),

        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();
      const version = this.teamContext.selectionVersion();
      untracked(() => {
        this.cancelMutations$.next();
        this.loadedSelectionVersion = null;
        this.updatingActionId.set(null);
        this.pagination.reset();
        this.error.set(false);
        this.data.set(null);
        this.actions.set([]);
        this.loading.set(false);
        this.loadActions$.next(teamId ? { teamId, version } : null);
      });
    });
  }

  retry(): void {
    const teamId = this.teamContext.selectedTeamId();
    this.loadActions$.next(
      teamId ? { teamId, version: this.teamContext.selectionVersion() } : null,
    );
  }

  changePage(page: number): void {
    if (this.pagination.changePage(page)) this.retry();
  }

  changePageSize(size: number): void {
    if (this.pagination.changePageSize(size)) this.retry();
  }

  updateStatus(action: EngineeringAction, status: ActionStatus): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || action.status === status) {
      return;
    }

    this.updateAction(teamId, action, { status });
  }

  updateOwner(action: EngineeringAction, owner: string): void {
    const teamId = this.teamContext.selectedTeamId();
    const normalizedOwner = owner.trim();

    if (!teamId || !normalizedOwner || normalizedOwner === action.owner) {
      return;
    }

    this.updateAction(teamId, action, {
      owner: normalizedOwner,
    });
  }

  updateDueDate(action: EngineeringAction, dueDate: Date | null): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || !dueDate) {
      return;
    }

    const formattedDueDate = this.formatDateOnly(dueDate);

    if (formattedDueDate === action.dueDate) {
      return;
    }

    this.updateAction(teamId, action, {
      dueDate: formattedDueDate,
    });
  }

  dueState(action: EngineeringActionView): 'overdue' | 'soon' | 'scheduled' | null {
    if (!action.dueDateValue || action.status === 'Done' || action.status === 'Cancelled')
      return null;

    const asOfDate = this.data()?.summary.asOfDate;
    if (!asOfDate || !action.dueDate) return null;
    const days =
      (Date.parse(`${action.dueDate}T00:00:00Z`) - Date.parse(`${asOfDate}T00:00:00Z`)) /
      86_400_000;

    if (days < 0) return 'overdue';
    if (days <= 3) return 'soon';
    return 'scheduled';
  }

  dueLabel(action: EngineeringActionView): string | null {
    const state = this.dueState(action);
    if (state === 'overdue') return this.i18n.t('actions.overdue');
    if (state === 'soon') return this.i18n.t('actions.dueSoon');
    return null;
  }

  metricLabel(metricType: string): string {
    const knownMetrics = [
      'CycleTime',
      'PRReviewTime',
      'DeploymentFrequency',
      'ChangeFailureRate',
      'LeadTime',
      'OpenPRs',
      'MergedPRs',
      'BlockedItems',
    ];
    return knownMetrics.includes(metricType) ? this.i18n.t(`metrics.${metricType}`) : metricType;
  }

  statusLabel(status: ActionStatus): string {
    return this.i18n.t(`statuses.${status}`);
  }

  private updateAction(
    teamId: string,
    action: EngineeringAction,
    request: UpdateEngineeringActionRequest,
  ): void {
    if (
      this.updatingActionId() ||
      this.loadedSelectionVersion !== this.teamContext.selectionVersion()
    )
      return;
    const version = this.teamContext.selectionVersion();
    this.updatingActionId.set(action.id);

    this.actionsApi
      .updateAction(teamId, action.id, request)
      .pipe(takeUntil(this.cancelMutations$), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updatedAction) => {
          if (version !== this.teamContext.selectionVersion()) return;
          const updatedActionView = this.toActionView(updatedAction);

          this.actions.update((actions) =>
            actions.map((current) =>
              current.id === updatedAction.id ? updatedActionView : current,
            ),
          );

          this.updatingActionId.set(null);
          this.message.success(this.i18n.t('actions.updated'));
          this.retry();
        },
        error: (error) => {
          if (version !== this.teamContext.selectionVersion()) return;
          console.error('Failed to update engineering action', error);

          this.updatingActionId.set(null);
          this.message.error(this.i18n.t('actions.updateError'));
        },
      });
  }

  private formatDateOnly(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private toActionView(action: EngineeringAction): EngineeringActionView {
    return {
      ...action,
      dueDateValue: this.parseDateOnly(action.dueDate),
    };
  }

  private parseDateOnly(value: string | null): Date | null {
    if (!value) {
      return null;
    }

    const [year, month, day] = value.split('-').map(Number);

    return new Date(year, month - 1, day);
  }
}
