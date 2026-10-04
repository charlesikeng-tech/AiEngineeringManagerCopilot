import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';

import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';

import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { TranslatePipe } from '@ngx-translate/core';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';

import { NzAlertModule } from 'ng-zorro-antd/alert';

import { NzButtonModule } from 'ng-zorro-antd/button';

import { NzDrawerModule } from 'ng-zorro-antd/drawer';

import { NzEmptyModule } from 'ng-zorro-antd/empty';

import { NzFormModule } from 'ng-zorro-antd/form';

import { NzIconModule } from 'ng-zorro-antd/icon';

import { NzInputModule } from 'ng-zorro-antd/input';

import { NzMessageService } from 'ng-zorro-antd/message';

import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';

import { NzSelectModule } from 'ng-zorro-antd/select';

import { NzSpinModule } from 'ng-zorro-antd/spin';

import { EMPTY, merge, Subject, Subscription } from 'rxjs';

import { catchError, switchMap, tap } from 'rxjs/operators';

import { TeamContext } from '@core/team/team-context';
import { PaginationState } from '@core/models/pagination-state';
import { TablePagination } from '@core/components/table-pagination/table-pagination';

import {
  CreateTeamMemberRequest,
  TeamMember,
  TeamMemberRole,
  UpdateTeamMemberRequest,
} from '@domains/teams/models/team-member';
import { Team, UpdateTeamRequest } from '@domains/teams/models/team';

import type { EngineeringMetric, MetricDataStatus, MetricType } from '@domains/engineering/models/metric';

import { ReportsApi } from '@features/reports/services/reports-api';

import { MetricsApi } from '../../services/metrics-api';

import { TeamApi } from '@domains/teams/data-access/team-api';

@Component({
  selector: 'app-team',

  standalone: true,

  imports: [
    TablePagination,
    TranslatePipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,

    NzAlertModule,

    NzEmptyModule,

    NzSpinModule,

    NzButtonModule,

    NzIconModule,

    ReactiveFormsModule,

    NzDrawerModule,

    NzFormModule,

    NzInputModule,

    NzSelectModule,

    NzPopconfirmModule,
  ],

  templateUrl: './team.html',

  styleUrl: './team.scss',

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPage {
  readonly i18n = inject(I18nService);

  private readonly teamApi = inject(TeamApi);

  private readonly reportsApi = inject(ReportsApi);

  private readonly metricsApi = inject(MetricsApi);

  private readonly teamContext = inject(TeamContext);

  private readonly destroyRef = inject(DestroyRef);

  private readonly message = inject(NzMessageService);

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  // ---------------------------------------------------------------------------

  // Team

  // ---------------------------------------------------------------------------

  readonly loading = signal(false);

  readonly error = signal(false);

  readonly team = signal<Team | null>(null);

  readonly members = signal<readonly TeamMember[]>([]);
  readonly memberPagination = new PaginationState();
  readonly membersLoading = signal(false);
  readonly membersError = signal(false);
  private memberLoadSubscription?: Subscription;
  private loadedSelectionVersion = -1;
  private readonly reloadSelection = new Subject<number>();

  readonly editTeamOpen = signal(false);

  readonly updatingTeam = signal(false);

  readonly deletingTeam = signal(false);

  readonly editTeamForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,

      validators: [Validators.required, Validators.maxLength(200)],
    }),

    description: new FormControl('', {
      nonNullable: true,

      validators: [Validators.maxLength(2000)],
    }),
  });

  // ---------------------------------------------------------------------------

  // Members

  // ---------------------------------------------------------------------------

  readonly addMemberOpen = signal(false);

  readonly creatingMember = signal(false);

  readonly deletingMemberId = signal<string | null>(null);

  readonly editingMember = signal<TeamMember | null>(null);

  readonly memberRoles: readonly {
    value: TeamMemberRole;

    label: string;
  }[] = [
    { value: 'EngineeringManager', label: 'team.roles.EngineeringManager' },

    { value: 'Developer', label: 'team.roles.Developer' },

    { value: 'TechLead', label: 'team.roles.TechLead' },

    { value: 'QA', label: 'team.roles.QA' },

    { value: 'ProductManager', label: 'team.roles.ProductManager' },

    { value: 'DataEngineer', label: 'team.roles.DataEngineer' },

    { value: 'Other', label: 'team.roles.Other' },
  ];

  readonly addMemberForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,

      validators: [Validators.required, Validators.maxLength(200)],
    }),

    email: new FormControl('', {
      nonNullable: true,

      validators: [Validators.required, Validators.email, Validators.maxLength(320)],
    }),

    role: new FormControl<TeamMemberRole>('Developer', {
      nonNullable: true,

      validators: [Validators.required],
    }),

    providerUserId: new FormControl('', {
      nonNullable: true,

      validators: [Validators.maxLength(200)],
    }),
  });

  readonly memberMutationInProgress = () =>
    this.creatingMember() || this.deletingMemberId() !== null;

  // ---------------------------------------------------------------------------
  // Engineering metrics / reports
  // ---------------------------------------------------------------------------

  readonly metrics = signal<readonly EngineeringMetric[]>([]);
  readonly calculatingMetrics = signal(false);
  readonly metricsError = signal(false);

  readonly calculatedMetricsPeriod = signal<{
    periodStart: string;
    periodEnd: string;
  } | null>(null);

  readonly generatingReport = signal(false);
  readonly reportGenerationError = signal(false);

  readonly metricTypes: readonly MetricType[] = [
    'CycleTime',
    'PRReviewTime',
    'DeploymentFrequency',
    'ChangeFailureRate',
    'LeadTime',
    'OpenPRs',
    'MergedPRs',
    'BlockedItems',
  ];

  readonly metricsPeriodForm = new FormGroup({
    periodStart: new FormControl(this.defaultPeriodStart(), {
      nonNullable: true,
      validators: [Validators.required],
    }),

    periodEnd: new FormControl(this.today(), {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  hasValidCalculatedMetrics(): boolean {
    const calculatedPeriod = this.calculatedMetricsPeriod();

    if (!calculatedPeriod || this.metrics().length === 0) {
      return false;
    }

    const { periodStart, periodEnd } = this.metricsPeriodForm.getRawValue();

    return calculatedPeriod.periodStart === periodStart && calculatedPeriod.periodEnd === periodEnd;
  }

  generateReport(): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();
    const calculatedPeriod = this.calculatedMetricsPeriod();

    if (
      !teamId ||
      this.loadedSelectionVersion !== version ||
      !calculatedPeriod ||
      !this.hasValidCalculatedMetrics() ||
      this.generatingReport()
    ) {
      return;
    }

    this.generatingReport.set(true);
    this.reportGenerationError.set(false);

    this.reportsApi
      .generateReport(teamId, calculatedPeriod.periodStart, calculatedPeriod.periodEnd)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          if (!this.isCurrentSelection(teamId, version)) {
            return;
          }

          this.generatingReport.set(false);
          this.message.success(this.i18n.t('team.notifications.reportGenerated'));

          void this.router.navigate(['/reports', report.id]);
        },

        error: (error) => {
          if (!this.isCurrentSelection(teamId, version)) {
            return;
          }

          console.error('Failed to generate engineering report', error);

          this.generatingReport.set(false);
          this.reportGenerationError.set(true);

          this.message.error(this.i18n.t('team.notifications.reportFailed'));
        },
      });
  }

  // ---------------------------------------------------------------------------

  // Constructor / loading

  // ---------------------------------------------------------------------------

  constructor() {
    const routeTeamId = this.route.snapshot.paramMap.get('teamId');

    if (routeTeamId) {
      this.teamContext.selectTeam(routeTeamId);
    }

    merge(toObservable(this.teamContext.selectionVersion), this.reloadSelection)
      .pipe(
        tap(() => {
          this.loadedSelectionVersion = -1;
          this.memberLoadSubscription?.unsubscribe();
          this.memberPagination.reset();
          this.membersLoading.set(false);
          this.membersError.set(false);
          this.addMemberOpen.set(false);
          this.editingMember.set(null);
          this.creatingMember.set(false);
          this.deletingMemberId.set(null);
          this.editTeamOpen.set(false);
          this.updatingTeam.set(false);
          this.deletingTeam.set(false);
          this.error.set(false);

          this.team.set(null);

          this.members.set([]);

          this.loading.set(!!this.teamContext.selectedTeamId());

          this.resetMetricsState();
        }),

        switchMap((version) => {
          const teamId = this.teamContext.selectedTeamId();
          if (!teamId) {
            return EMPTY;
          }

          return this.teamApi.getTeam(teamId).pipe(
            tap((team) => {
              if (!this.isCurrentSelection(teamId, version)) return;
              this.loadedSelectionVersion = version;
              this.team.set(team);
              this.loading.set(false);
              this.loadMembers();
            }),

            catchError((error) => {
              if (!this.isCurrentSelection(teamId, version)) return EMPTY;
              console.error('Failed to load team', error);

              this.message.error(this.i18n.t('team.notifications.loadFailed'));

              this.error.set(true);

              this.loading.set(false);

              return EMPTY;
            }),
          );
        }),

        takeUntilDestroyed(this.destroyRef),
      )

      .subscribe();
  }

  retryTeam(): void {
    this.reloadSelection.next(this.teamContext.selectionVersion());
  }

  loadMembers(): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();
    if (!teamId || this.team()?.id !== teamId || this.loadedSelectionVersion !== version) return;
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
            if (!this.isCurrentSelection(teamId, version)) return;
            if (this.memberPagination.acceptTotal(page.totalCount)) {
              this.loadMembers();
              return;
            }
            this.members.set(page.items);
            this.membersLoading.set(false);
          },
          error: (error) => {
            if (!this.isCurrentSelection(teamId, version)) return;
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

  private isCurrentSelection(teamId: string, version: number): boolean {
    return (
      this.teamContext.selectedTeamId() === teamId &&
      this.teamContext.selectionVersion() === version
    );
  }

  // ---------------------------------------------------------------------------

  // Members

  // ---------------------------------------------------------------------------

  roleLabel(role: TeamMember['role']): string {
    return this.i18n.t(`team.roles.${role}`);
  }

  openAddMember(): void {
    if (
      this.team()?.id !== this.teamContext.selectedTeamId() ||
      this.loadedSelectionVersion !== this.teamContext.selectionVersion()
    )
      return;
    this.editingMember.set(null);

    this.addMemberForm.reset({
      name: '',

      email: '',

      role: 'Developer',

      providerUserId: '',
    });

    this.addMemberOpen.set(true);
  }

  closeAddMember(): void {
    if (this.creatingMember()) {
      return;
    }

    this.addMemberOpen.set(false);

    this.editingMember.set(null);
  }

  saveMember(): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();

    if (
      !teamId ||
      this.team()?.id !== teamId ||
      this.loadedSelectionVersion !== version ||
      this.memberMutationInProgress() ||
      this.addMemberForm.invalid
    ) {
      this.addMemberForm.markAllAsTouched();

      return;
    }

    const value = this.addMemberForm.getRawValue();

    const providerUserId = value.providerUserId.trim();

    const request: CreateTeamMemberRequest | UpdateTeamMemberRequest = {
      name: value.name.trim(),

      email: value.email.trim(),

      role: value.role,

      providerUserId: providerUserId || null,
    };

    const editingMember = this.editingMember();
    if (editingMember && editingMember.teamId !== teamId) return;

    this.creatingMember.set(true);

    const operation$ = editingMember
      ? this.teamApi.updateMember(teamId, editingMember.id, request)
      : this.teamApi.createMember(teamId, request);

    operation$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (!this.isCurrentSelection(teamId, version)) return;
        this.loadMembers();

        this.creatingMember.set(false);

        this.addMemberOpen.set(false);

        this.editingMember.set(null);

        this.addMemberForm.reset({
          name: '',

          email: '',

          role: 'Developer',

          providerUserId: '',
        });

        this.message.success(
          this.i18n.t(
            editingMember ? 'team.notifications.memberUpdated' : 'team.notifications.memberAdded',
          ),
        );
      },

      error: (error) => {
        if (!this.isCurrentSelection(teamId, version)) return;
        console.error('Failed to save team member', error);

        this.message.error(
          this.i18n.t(
            editingMember
              ? 'team.notifications.memberUpdateFailed'
              : 'team.notifications.memberAddFailed',
          ),
        );

        this.creatingMember.set(false);
      },
    });
  }

  openEditMember(member: TeamMember): void {
    if (
      member.teamId !== this.teamContext.selectedTeamId() ||
      this.team()?.id !== member.teamId ||
      this.loadedSelectionVersion !== this.teamContext.selectionVersion()
    )
      return;
    this.editingMember.set(member);

    this.addMemberForm.reset({
      name: member.name,

      email: member.email,

      role: member.role,

      providerUserId: member.providerUserId ?? '',
    });

    this.addMemberOpen.set(true);
  }

  deleteMember(member: TeamMember): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();

    if (
      !teamId ||
      this.team()?.id !== teamId ||
      this.loadedSelectionVersion !== version ||
      member.teamId !== teamId ||
      this.memberMutationInProgress()
    ) {
      return;
    }

    this.deletingMemberId.set(member.id);

    this.teamApi

      .deleteMember(teamId, member.id)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: () => {
          if (!this.isCurrentSelection(teamId, version)) return;
          this.loadMembers();

          this.deletingMemberId.set(null);

          this.message.success(this.i18n.t('team.notifications.memberRemoved'));
        },

        error: (error) => {
          if (!this.isCurrentSelection(teamId, version)) return;
          console.error('Failed to remove team member', error);

          this.message.error(this.i18n.t('team.notifications.memberRemoveFailed'));

          this.deletingMemberId.set(null);
        },
      });
  }

  // ---------------------------------------------------------------------------

  // Team

  // ---------------------------------------------------------------------------

  openEditTeam(): void {
    const currentTeam = this.team();

    if (!currentTeam) {
      return;
    }

    this.editTeamForm.reset({
      name: currentTeam.name,

      description: currentTeam.description ?? '',
    });

    this.editTeamOpen.set(true);
  }

  closeEditTeam(): void {
    if (this.updatingTeam()) {
      return;
    }

    this.editTeamOpen.set(false);
  }

  updateTeam(): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();

    if (
      !teamId ||
      this.team()?.id !== teamId ||
      this.loadedSelectionVersion !== version ||
      this.updatingTeam() ||
      this.editTeamForm.invalid
    ) {
      this.editTeamForm.markAllAsTouched();

      return;
    }

    const value = this.editTeamForm.getRawValue();

    const description = value.description.trim();

    const request: UpdateTeamRequest = {
      name: value.name.trim(),

      description: description || null,
    };

    this.updatingTeam.set(true);

    this.teamApi

      .updateTeam(teamId, request)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: (updatedTeam) => {
          if (!this.isCurrentSelection(teamId, version)) return;
          this.team.set(updatedTeam);

          this.updatingTeam.set(false);

          this.editTeamOpen.set(false);

          this.message.success(this.i18n.t('team.notifications.teamUpdated'));
        },

        error: (error) => {
          if (!this.isCurrentSelection(teamId, version)) return;
          console.error('Failed to update team', error);

          this.updatingTeam.set(false);

          this.message.error(this.i18n.t('team.notifications.teamUpdateFailed'));
        },
      });
  }

  deleteTeam(): void {
    const currentTeam = this.team();
    const version = this.teamContext.selectionVersion();

    if (
      !currentTeam ||
      !this.isCurrentSelection(currentTeam.id, version) ||
      this.loadedSelectionVersion !== version ||
      this.deletingTeam()
    ) {
      return;
    }

    this.deletingTeam.set(true);

    this.teamApi

      .deleteTeam(currentTeam.id)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: () => {
          if (!this.isCurrentSelection(currentTeam.id, version)) return;
          this.deletingTeam.set(false);

          this.teamContext.clearTeam();

          this.message.success(this.i18n.t('team.notifications.teamDeleted'));

          void this.router.navigate(['/team']);
        },

        error: (error) => {
          if (!this.isCurrentSelection(currentTeam.id, version)) return;
          console.error('Failed to delete team', error);

          this.deletingTeam.set(false);

          this.message.error(this.i18n.t('team.notifications.teamDeleteFailed'));
        },
      });
  }

  // ---------------------------------------------------------------------------

  // Engineering metrics

  // ---------------------------------------------------------------------------

  calculateMetrics(): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();

    if (
      !teamId ||
      this.loadedSelectionVersion !== version ||
      this.metricsPeriodForm.invalid ||
      this.calculatingMetrics() ||
      this.generatingReport()
    ) {
      this.metricsPeriodForm.markAllAsTouched();
      return;
    }

    const { periodStart, periodEnd } = this.metricsPeriodForm.getRawValue();

    if (periodStart > periodEnd) {
      this.message.warning(this.i18n.t('team.notifications.invalidPeriod'));
      return;
    }

    this.calculatingMetrics.set(true);
    this.metricsError.set(false);
    this.reportGenerationError.set(false);

    this.metricsApi
      .calculate(teamId, periodStart, periodEnd)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (metrics) => {
          if (!this.isCurrentSelection(teamId, version)) {
            return;
          }

          this.metrics.set(metrics);
          this.calculatedMetricsPeriod.set({
            periodStart,
            periodEnd,
          });
          this.calculatingMetrics.set(false);

          const available = metrics.filter((metric) => metric.dataStatus === 'Available').length;

          this.message.success(
            this.i18n.t('team.notifications.metricsCalculated', {
              available: this.i18n.formatNumber(available),
              total: this.i18n.formatNumber(metrics.length),
            }),
          );
        },

        error: (error) => {
          if (!this.isCurrentSelection(teamId, version)) {
            return;
          }

          console.error('Failed to calculate engineering metrics', error);

          this.calculatingMetrics.set(false);
          this.metricsError.set(true);

          this.message.error(this.i18n.t('team.notifications.metricsFailed'));
        },
      });
  }

  metric(metricType: MetricType): EngineeringMetric | null {
    return this.metrics().find((metric) => metric.metricType === metricType) ?? null;
  }

  metricLabel(metricType: MetricType): string {
    return this.i18n.t(`metrics.${metricType}`);
  }

  metricStatusLabel(status: MetricDataStatus): string {
    return this.i18n.t(`team.metricStatuses.${status}`);
  }

  metricValue(metric: EngineeringMetric): string {
    if (metric.dataStatus !== 'Available' || metric.value === null) {
      return '—';
    }

    const value = this.i18n.formatNumber(metric.value, '1.0-20');

    switch (metric.metricType) {
      case 'CycleTime':

      case 'PRReviewTime':

      case 'LeadTime':
        return this.i18n.t('team.metricHours', { value });

      case 'ChangeFailureRate':
        return this.i18n.t('team.metricPercent', { value });

      case 'DeploymentFrequency':

      case 'OpenPRs':

      case 'MergedPRs':

      case 'BlockedItems':
        return value;
    }
  }

  private today(): string {
    return this.toDateInputValue(new Date());
  }

  private defaultPeriodStart(): string {
    const date = new Date();

    date.setDate(date.getDate() - 29);

    return this.toDateInputValue(date);
  }

  private toDateInputValue(date: Date): string {
    const year = date.getFullYear();

    const month = String(date.getMonth() + 1).padStart(2, '0');

    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private resetMetricsState(): void {
    this.metrics.set([]);
    this.calculatedMetricsPeriod.set(null);

    this.calculatingMetrics.set(false);
    this.metricsError.set(false);

    this.generatingReport.set(false);
    this.reportGenerationError.set(false);

    this.metricsPeriodForm.reset({
      periodStart: this.defaultPeriodStart(),
      periodEnd: this.today(),
    });
  }

  availableMetricCount(): number {
    return this.metrics().filter((metric) => metric.dataStatus === 'Available').length;
  }
}
