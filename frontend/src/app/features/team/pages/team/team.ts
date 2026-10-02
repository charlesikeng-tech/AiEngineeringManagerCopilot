import { HttpErrorResponse } from '@angular/common/http';

import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';

import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';

import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { DatePipe } from '@angular/common';

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

import { EMPTY } from 'rxjs';

import { catchError, switchMap, tap } from 'rxjs/operators';

import { TeamContext } from '@core/team/team-context';

import { CreateGitHubConnectionRequest } from '../../models/create-github-connection-request';

import { GitHubConnection, GitHubOwnerType } from '../../models/github-connection';

import { GitHubConnectionTestResponse } from '../../models/github-connection-test-response';

import { GitHubSyncResponse } from '../../models/github-sync-response';

import {
  CreateTeamMemberRequest,
  Team,
  TeamMember,
  TeamMemberRole,
  UpdateTeamMemberRequest,
  UpdateTeamRequest,
} from '../../models/team.model';

import { EngineeringMetric, MetricDataStatus, MetricType } from '../../models/engineering-metric';

import { ReportsApi } from '@features/reports/services/reports-api';

import { GitHubApi } from '../../services/github-api';

import { MetricsApi } from '../../services/metrics-api';

import { TeamApi } from '../../services/team-api';

@Component({
  selector: 'app-team',

  standalone: true,

  imports: [
    DatePipe,

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
  private readonly teamApi = inject(TeamApi);

  private readonly reportsApi = inject(ReportsApi);

  private readonly metricsApi = inject(MetricsApi);

  private readonly githubApi = inject(GitHubApi);

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
    { value: 'EngineeringManager', label: 'Engineering Manager' },

    { value: 'Developer', label: 'Developer' },

    { value: 'TechLead', label: 'Tech Lead' },

    { value: 'QA', label: 'QA' },

    { value: 'ProductManager', label: 'Product Manager' },

    { value: 'DataEngineer', label: 'Data Engineer' },

    { value: 'Other', label: 'Other' },
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

  // GitHub

  // ---------------------------------------------------------------------------

  readonly githubConnection = signal<GitHubConnection | null>(null);

  readonly githubConnectionLoading = signal(false);

  readonly githubConnectionError = signal(false);

  readonly githubConnectOpen = signal(false);

  readonly githubConnecting = signal(false);

  readonly githubTesting = signal(false);

  readonly githubTestResult = signal<GitHubConnectionTestResponse | null>(null);

  readonly githubSyncing = signal(false);

  readonly githubSyncResult = signal<GitHubSyncResponse | null>(null);

  readonly githubDisconnecting = signal(false);

  readonly githubOwnerTypes: readonly {
    value: GitHubOwnerType;

    label: string;
  }[] = [
    { value: 'User', label: 'Personal account' },

    { value: 'Organization', label: 'Organization' },
  ];

  readonly githubConnectionForm = new FormGroup({
    owner: new FormControl('', {
      nonNullable: true,

      validators: [Validators.required, Validators.maxLength(200)],
    }),

    ownerType: new FormControl<GitHubOwnerType>('User', {
      nonNullable: true,

      validators: [Validators.required],
    }),

    accessToken: new FormControl('', {
      nonNullable: true,

      validators: [Validators.required],
    }),
  });

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
    const calculatedPeriod = this.calculatedMetricsPeriod();

    if (
      !teamId ||
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
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          this.generatingReport.set(false);
          this.message.success('Engineering report generated successfully.');

          void this.router.navigate(['/reports', report.id]);
        },

        error: (error) => {
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          console.error('Failed to generate engineering report', error);

          this.generatingReport.set(false);
          this.reportGenerationError.set(true);

          this.message.error('Unable to generate the engineering report.');
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

    toObservable(this.teamContext.selectedTeamId)
      .pipe(
        tap((teamId) => {
          this.error.set(false);

          this.team.set(null);

          this.members.set([]);

          this.resetGitHubState();

          this.loading.set(!!teamId);

          this.resetMetricsState();
        }),

        switchMap((teamId) => {
          if (!teamId) {
            return EMPTY;
          }

          return this.teamApi.getTeam(teamId).pipe(
            switchMap((team) =>
              this.teamApi.getMembers(teamId).pipe(
                tap((members) => {
                  this.team.set(team);

                  this.members.set(members);

                  this.loading.set(false);

                  this.loadGitHubConnection(teamId);
                }),
              ),
            ),

            catchError((error) => {
              console.error('Failed to load team', error);

              this.message.error('Unable to load the team. Please try again.');

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

  // ---------------------------------------------------------------------------

  // Members

  // ---------------------------------------------------------------------------

  roleLabel(role: TeamMember['role']): string {
    const labels: Record<TeamMember['role'], string> = {
      EngineeringManager: 'Engineering Manager',

      Developer: 'Developer',

      TechLead: 'Tech Lead',

      QA: 'QA',

      ProductManager: 'Product Manager',

      DataEngineer: 'Data Engineer',

      Other: 'Other',
    };

    return labels[role];
  }

  openAddMember(): void {
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

    if (!teamId || this.addMemberForm.invalid) {
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

    this.creatingMember.set(true);

    const operation$ = editingMember
      ? this.teamApi.updateMember(teamId, editingMember.id, request)
      : this.teamApi.createMember(teamId, request);

    operation$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (savedMember) => {
        if (editingMember) {
          this.members.update((members) =>
            members.map((member) => (member.id === savedMember.id ? savedMember : member)),
          );
        } else {
          this.members.update((members) => [...members, savedMember]);
        }

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
          editingMember ? 'Team member updated successfully.' : 'Team member added successfully.',
        );
      },

      error: (error) => {
        console.error('Failed to save team member', error);

        this.message.error(
          editingMember ? 'Unable to update the team member.' : 'Unable to add the team member.',
        );

        this.creatingMember.set(false);
      },
    });
  }

  openEditMember(member: TeamMember): void {
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

    if (!teamId) {
      return;
    }

    this.deletingMemberId.set(member.id);

    this.teamApi

      .deleteMember(teamId, member.id)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: () => {
          this.members.update((members) => members.filter((current) => current.id !== member.id));

          this.deletingMemberId.set(null);

          this.message.success('Team member removed successfully.');
        },

        error: (error) => {
          console.error('Failed to remove team member', error);

          this.message.error('Unable to remove the team member.');

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

    if (!teamId || this.editTeamForm.invalid) {
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
          this.team.set(updatedTeam);

          this.updatingTeam.set(false);

          this.editTeamOpen.set(false);

          this.message.success('Team updated successfully.');
        },

        error: (error) => {
          console.error('Failed to update team', error);

          this.updatingTeam.set(false);

          this.message.error('Unable to update the team.');
        },
      });
  }

  deleteTeam(): void {
    const currentTeam = this.team();

    if (!currentTeam) {
      return;
    }

    this.deletingTeam.set(true);

    this.teamApi

      .deleteTeam(currentTeam.id)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: () => {
          this.deletingTeam.set(false);

          this.teamContext.clearTeam();

          this.message.success('Team deleted successfully.');

          void this.router.navigate(['/team']);
        },

        error: (error) => {
          console.error('Failed to delete team', error);

          this.deletingTeam.set(false);

          this.message.error('Unable to delete the team.');
        },
      });
  }

  // ---------------------------------------------------------------------------

  // GitHub

  // ---------------------------------------------------------------------------

  reloadGitHubConnection(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId) {
      return;
    }

    this.loadGitHubConnection(teamId);
  }

  openGitHubConnection(): void {
    if (this.githubConnection()) {
      return;
    }

    this.githubConnectionForm.reset({
      owner: '',

      ownerType: 'User',

      accessToken: '',
    });

    this.githubTestResult.set(null);

    this.githubSyncResult.set(null);

    this.githubConnectOpen.set(true);
  }

  closeGitHubConnection(): void {
    if (this.githubConnecting()) {
      return;
    }

    this.githubConnectOpen.set(false);

    this.githubConnectionForm.reset({
      owner: '',

      ownerType: 'User',

      accessToken: '',
    });
  }

  connectGitHub(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || this.githubConnectionForm.invalid) {
      this.githubConnectionForm.markAllAsTouched();

      return;
    }

    const value = this.githubConnectionForm.getRawValue();

    const owner = value.owner.trim();

    const accessToken = value.accessToken.trim();

    if (!owner || !accessToken) {
      this.githubConnectionForm.markAllAsTouched();

      return;
    }

    const request: CreateGitHubConnectionRequest = {
      owner,

      ownerType: value.ownerType,

      accessToken,
    };

    this.githubConnecting.set(true);

    this.githubTestResult.set(null);

    this.githubSyncResult.set(null);

    this.githubApi

      .createConnection(teamId, request)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: (connection) => {
          this.githubConnection.set(connection);

          this.githubConnecting.set(false);

          this.githubConnectOpen.set(false);

          // Never keep the PAT in the form after submission.

          this.githubConnectionForm.reset({
            owner: '',

            ownerType: 'User',

            accessToken: '',
          });

          this.message.success('GitHub connected successfully.');
        },

        error: (error) => {
          console.error('Failed to connect GitHub', error);

          this.githubConnecting.set(false);

          this.message.error('Unable to connect GitHub.');
        },
      });
  }

  testGitHubConnection(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || !this.githubConnection() || this.githubTesting()) {
      return;
    }

    this.githubTesting.set(true);

    this.githubTestResult.set(null);

    this.githubApi

      .testConnection(teamId)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: (result) => {
          this.githubTestResult.set(result);

          this.githubTesting.set(false);

          if (result.success) {
            this.message.success(result.message || 'GitHub connection is valid.');

            return;
          }

          this.message.warning(result.message || 'GitHub connection test failed.');
        },

        error: (error) => {
          console.error('Failed to test GitHub connection', error);

          this.githubTesting.set(false);

          this.message.error('Unable to test GitHub connection.');
        },
      });
  }

  syncGitHub(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || !this.githubConnection() || this.githubSyncing()) {
      return;
    }

    this.githubSyncing.set(true);

    this.githubSyncResult.set(null);

    this.githubApi

      .sync(teamId)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: (result) => {
          this.githubSyncResult.set(result);

          this.githubSyncing.set(false);

          this.message.success(
            `GitHub synchronized: ${result.synchronized} repository${
              result.synchronized === 1 ? '' : 'ies'
            } synchronized.`,
          );

          // Refresh the connection from the backend because

          // LastSyncAt is owned by the server.

          this.refreshGitHubConnection(teamId);
        },

        error: (error) => {
          console.error('Failed to synchronize GitHub', error);

          this.githubSyncing.set(false);

          this.message.error('Unable to synchronize GitHub.');
        },
      });
  }

  disconnectGitHub(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || !this.githubConnection() || this.githubDisconnecting()) {
      return;
    }

    this.githubDisconnecting.set(true);

    this.githubApi

      .deleteConnection(teamId)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: () => {
          this.githubConnection.set(null);

          this.githubTestResult.set(null);

          this.githubSyncResult.set(null);

          this.githubDisconnecting.set(false);

          this.message.success('GitHub disconnected successfully.');
        },

        error: (error) => {
          console.error('Failed to disconnect GitHub', error);

          this.githubDisconnecting.set(false);

          this.message.error('Unable to disconnect GitHub.');
        },
      });
  }

  // ---------------------------------------------------------------------------

  // Engineering metrics

  // ---------------------------------------------------------------------------

  calculateMetrics(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (
      !teamId ||
      this.metricsPeriodForm.invalid ||
      this.calculatingMetrics() ||
      this.generatingReport()
    ) {
      this.metricsPeriodForm.markAllAsTouched();
      return;
    }

    const { periodStart, periodEnd } = this.metricsPeriodForm.getRawValue();

    if (periodStart > periodEnd) {
      this.message.warning('The start date must be before the end date.');
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
          if (this.teamContext.selectedTeamId() !== teamId) {
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
            `Metrics calculated successfully. ${available}/${metrics.length} metrics available.`,
          );
        },

        error: (error) => {
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          console.error('Failed to calculate engineering metrics', error);

          this.calculatingMetrics.set(false);
          this.metricsError.set(true);

          this.message.error('Unable to calculate engineering metrics.');
        },
      });
  }

  // ---------------------------------------------------------------------------

  // GitHub internal loading

  // ---------------------------------------------------------------------------

  private loadGitHubConnection(teamId: string): void {
    this.githubConnectionLoading.set(true);

    this.githubConnectionError.set(false);

    this.githubConnection.set(null);

    this.githubTestResult.set(null);

    this.githubSyncResult.set(null);

    this.githubApi

      .getConnection(teamId)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: (connection) => {
          // Ignore a late response if the selected team changed.

          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          this.githubConnection.set(connection);

          this.githubConnectionLoading.set(false);
        },

        error: (error: HttpErrorResponse) => {
          // Ignore a late response if the selected team changed.

          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          this.githubConnectionLoading.set(false);

          // A team without a GitHub connection is a normal state.

          if (error.status === 404) {
            this.githubConnection.set(null);

            return;
          }

          console.error('Failed to load GitHub connection', error);

          this.githubConnectionError.set(true);
        },
      });
  }

  private refreshGitHubConnection(teamId: string): void {
    this.githubApi

      .getConnection(teamId)

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({
        next: (connection) => {
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          this.githubConnection.set(connection);

          this.githubConnectionError.set(false);
        },

        error: (error: HttpErrorResponse) => {
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }

          console.error('Failed to refresh GitHub connection', error);

          this.githubConnectionError.set(true);
        },
      });
  }

  private resetGitHubState(): void {
    this.githubConnection.set(null);

    this.githubConnectionLoading.set(false);

    this.githubConnectionError.set(false);

    this.githubTestResult.set(null);

    this.githubSyncResult.set(null);

    this.githubTesting.set(false);

    this.githubSyncing.set(false);

    this.githubDisconnecting.set(false);

    this.githubConnectOpen.set(false);

    this.githubConnecting.set(false);

    this.githubConnectionForm.reset({
      owner: '',

      ownerType: 'User',

      accessToken: '',
    });
  }

  metric(metricType: MetricType): EngineeringMetric | null {
    return this.metrics().find((metric) => metric.metricType === metricType) ?? null;
  }

  metricLabel(metricType: MetricType): string {
    const labels: Record<MetricType, string> = {
      CycleTime: 'Cycle Time',

      PRReviewTime: 'PR Review Time',

      DeploymentFrequency: 'Deployment Frequency',

      ChangeFailureRate: 'Change Failure Rate',

      LeadTime: 'Lead Time',

      OpenPRs: 'Open PRs',

      MergedPRs: 'Merged PRs',

      BlockedItems: 'Blocked Items',
    };

    return labels[metricType];
  }

  metricStatusLabel(status: MetricDataStatus): string {
    const labels: Record<MetricDataStatus, string> = {
      Available: 'Available',

      NoData: 'No data',

      SourceNotConfigured: 'Source not configured',
    };

    return labels[status];
  }

  metricValue(metric: EngineeringMetric): string {
    if (metric.dataStatus !== 'Available' || metric.value === null) {
      return '—';
    }

    switch (metric.metricType) {
      case 'CycleTime':

      case 'PRReviewTime':

      case 'LeadTime':
        return `${metric.value}h`;

      case 'ChangeFailureRate':
        return `${metric.value}%`;

      case 'DeploymentFrequency':

      case 'OpenPRs':

      case 'MergedPRs':

      case 'BlockedItems':
        return `${metric.value}`;
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
