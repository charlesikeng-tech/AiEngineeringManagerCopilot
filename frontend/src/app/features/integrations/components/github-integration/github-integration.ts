import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { GitHubApi } from '../../providers/github/data-access/github-api';
import { GitHubConnection, GitHubOwnerType } from '../../providers/github/models/github-connection';
import { GitHubConnectionTestResponse } from '../../providers/github/models/github-connection-test-response';
import { GitHubSyncResponse } from '../../providers/github/models/github-sync-response';
import { IntegrationRequestScope } from '../../services/integration-request-scope';

@Component({
  selector: 'app-github-integration',
  imports: [
    TranslatePipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzDrawerModule,
    NzFormModule,
    NzIconModule,
    NzInputModule,
    NzPopconfirmModule,
    NzSelectModule,
    NzSpinModule,
  ],
  providers: [IntegrationRequestScope],
  templateUrl: './github-integration.html',
  styleUrl: './github-integration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GitHubIntegration {
  private readonly api = inject(GitHubApi);
  private readonly scope = inject(IntegrationRequestScope);
  private readonly teamContext = this.scope.teamContext;
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
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
  readonly githubOwnerTypes = [
    { value: 'User', label: 'team.ownerTypes.User' },
    { value: 'Organization', label: 'team.ownerTypes.Organization' },
  ] as const;
  readonly githubConnectionForm = new FormGroup({
    owner: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    ownerType: new FormControl<GitHubOwnerType>('User', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    accessToken: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.scope.watch(
      () => this.reset(),
      (teamId) => this.loadGitHubConnection(teamId),
    );
  }

  reloadGitHubConnection(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (teamId) this.loadGitHubConnection(teamId);
  }

  openGitHubConnection(): void {
    if (!this.teamContext.hasSelectedTeam() || this.githubConnection()) return;
    this.githubConnectionForm.reset({ owner: '', ownerType: 'User', accessToken: '' });
    this.githubTestResult.set(null);
    this.githubSyncResult.set(null);
    this.githubConnectOpen.set(true);
  }

  closeGitHubConnection(): void {
    if (this.githubConnecting()) return;
    this.githubConnectOpen.set(false);
    this.githubConnectionForm.reset({ owner: '', ownerType: 'User', accessToken: '' });
  }

  connectGitHub(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || this.githubConnectionForm.invalid || this.githubConnecting()) {
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
    this.githubConnecting.set(true);
    this.githubTestResult.set(null);
    this.githubSyncResult.set(null);
    this.api
      .createConnection(teamId, { owner, ownerType: value.ownerType, accessToken })
      .pipe(this.scope.request())
      .subscribe({
        next: (connection) => {
          this.githubConnection.set(connection);
          this.githubConnecting.set(false);
          this.githubConnectOpen.set(false);
          this.githubConnectionForm.reset({ owner: '', ownerType: 'User', accessToken: '' });
          this.message.success(this.i18n.t('team.notifications.githubConnected'));
        },
        error: (error) => {
          console.error('Failed to connect GitHub', error);
          this.githubConnecting.set(false);
          this.message.error(this.i18n.t('team.notifications.githubConnectFailed'));
        },
      });
  }

  testGitHubConnection(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.githubConnection() || this.githubTesting()) return;
    this.githubTesting.set(true);
    this.githubTestResult.set(null);
    this.api
      .testConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (result) => {
          this.githubTestResult.set(result);
          this.githubTesting.set(false);
          if (result.success) {
            this.message.success(result.message || this.i18n.t('team.notifications.githubValid'));
          } else {
            this.message.warning(
              result.message || this.i18n.t('team.notifications.githubTestFailed'),
            );
          }
        },
        error: (error) => {
          console.error('Failed to test GitHub connection', error);
          this.githubTesting.set(false);
          this.message.error(this.i18n.t('team.notifications.githubTestError'));
        },
      });
  }

  syncGitHub(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.githubConnection() || this.githubSyncing()) return;
    this.githubSyncing.set(true);
    this.githubSyncResult.set(null);
    this.api
      .sync(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (result) => {
          this.githubSyncResult.set(result);
          this.githubSyncing.set(false);
          this.message.success(
            this.i18n.t(
              result.synchronized === 1
                ? 'team.notifications.githubSyncedOne'
                : 'team.notifications.githubSyncedMany',
              { count: this.i18n.formatNumber(result.synchronized) },
            ),
          );
          this.refreshGitHubConnection(teamId);
        },
        error: (error) => {
          console.error('Failed to synchronize GitHub', error);
          this.githubSyncing.set(false);
          this.message.error(this.i18n.t('team.notifications.githubSyncFailed'));
        },
      });
  }

  disconnectGitHub(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.githubConnection() || this.githubDisconnecting()) return;
    this.githubDisconnecting.set(true);
    this.api
      .deleteConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: () => {
          this.githubConnection.set(null);
          this.githubTestResult.set(null);
          this.githubSyncResult.set(null);
          this.githubDisconnecting.set(false);
          this.message.success(this.i18n.t('team.notifications.githubDisconnected'));
        },
        error: (error) => {
          console.error('Failed to disconnect GitHub', error);
          this.githubDisconnecting.set(false);
          this.message.error(this.i18n.t('team.notifications.githubDisconnectFailed'));
        },
      });
  }

  private loadGitHubConnection(teamId: string): void {
    this.githubConnectionLoading.set(true);
    this.githubConnectionError.set(false);
    this.githubConnection.set(null);
    this.githubTestResult.set(null);
    this.githubSyncResult.set(null);
    this.api
      .getConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (connection) => {
          this.githubConnection.set(connection);
          this.githubConnectionLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.githubConnectionLoading.set(false);
          if (error.status === 404) return;
          console.error('Failed to load GitHub connection', error);
          this.githubConnectionError.set(true);
        },
      });
  }

  private refreshGitHubConnection(teamId: string): void {
    this.api
      .getConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (connection) => {
          this.githubConnection.set(connection);
          this.githubConnectionError.set(false);
        },
        error: (error: HttpErrorResponse) => {
          console.error('Failed to refresh GitHub connection', error);
          this.githubConnectionError.set(true);
        },
      });
  }

  private reset(): void {
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
    this.githubConnectionForm.reset({ owner: '', ownerType: 'User', accessToken: '' });
  }
}
