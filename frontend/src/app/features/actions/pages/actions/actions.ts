import { UpperCasePipe } from '@angular/common';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { EMPTY } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';

import { TeamContext } from '@core/team/team-context';
import {
  ActionStatus,
  EngineeringAction,
  EngineeringActionsResponse,
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

  readonly data = signal<EngineeringActionsResponse | null>(null);
  readonly actions = signal<readonly EngineeringActionView[]>([]);

  readonly todoCount = computed(
    () => this.actions().filter((action) => action.status === 'Todo').length,
  );

  readonly inProgressCount = computed(
    () => this.actions().filter((action) => action.status === 'InProgress').length,
  );

  readonly doneCount = computed(
    () => this.actions().filter((action) => action.status === 'Done').length,
  );

  readonly cancelledCount = computed(
    () => this.actions().filter((action) => action.status === 'Cancelled').length,
  );

  readonly totalCount = computed(() => this.actions().length);

  readonly sortedActions = computed(() => {
    const priorityOrder: Record<string, number> = { Critical: 0, High: 1, Medium: 2, Low: 3 };
    const statusOrder: Record<ActionStatus, number> = { InProgress: 0, Todo: 1, Done: 2, Cancelled: 3 };

    return [...this.actions()].sort((left, right) => {
      const priorityDifference =
        (priorityOrder[left.priority] ?? 99) - (priorityOrder[right.priority] ?? 99);

      return priorityDifference !== 0
        ? priorityDifference
        : statusOrder[left.status] - statusOrder[right.status];
    });
  });

  readonly overdueCount = computed(
    () => this.actions().filter((action) => this.dueState(action) === 'overdue').length,
  );

  constructor() {
    toObservable(this.teamContext.selectedTeamId)
      .pipe(
        tap((teamId) => {
          this.error.set(false);
          this.data.set(null);
          this.actions.set([]);
          this.loading.set(!!teamId);
        }),

        switchMap((teamId) => {
          if (!teamId) {
            return EMPTY;
          }

          return this.actionsApi.getCurrentActions(teamId).pipe(
            catchError((error) => {
              console.error('Failed to load engineering actions', error);

              this.error.set(true);
              this.loading.set(false);

              return EMPTY;
            }),
          );
        }),

        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((response) => {
        this.data.set(response);
        this.actions.set(response.actions.map((action) => this.toActionView(action)));
        this.loading.set(false);
      });
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
    if (!action.dueDateValue || action.status === 'Done' || action.status === 'Cancelled') return null;

    const today = this.startOfDay(new Date());
    const dueDate = this.startOfDay(action.dueDateValue);
    const days = Math.ceil((dueDate.getTime() - today.getTime()) / 86_400_000);

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
      'CycleTime', 'PRReviewTime', 'DeploymentFrequency', 'ChangeFailureRate',
      'LeadTime', 'OpenPRs', 'MergedPRs', 'BlockedItems',
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
    this.updatingActionId.set(action.id);

    this.actionsApi
      .updateAction(teamId, action.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updatedAction) => {
          const updatedActionView = this.toActionView(updatedAction);

          this.actions.update((actions) =>
            actions.map((current) =>
              current.id === updatedAction.id ? updatedActionView : current,
            ),
          );

          this.data.update((data) =>
            data
              ? {
                  ...data,
                  actions: data.actions.map((current) =>
                    current.id === updatedAction.id ? updatedAction : current,
                  ),
                }
              : null,
          );

          this.updatingActionId.set(null);
          this.message.success(this.i18n.t('actions.updated'));
        },
        error: (error) => {
          console.error('Failed to update engineering action', error);

          this.updatingActionId.set(null);
          this.message.error(this.i18n.t('actions.updateError'));
        },
      });
  }

  private startOfDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
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
