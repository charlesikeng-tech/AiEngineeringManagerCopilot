import { HttpErrorResponse } from '@angular/common/http';

import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
} from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

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

import { TeamContext } from '@core/team/team-context';
import { TeamSelector } from '@core/team/team-selector/team-selector';
import { JiraIntegration } from '../../components/jira-integration/jira-integration';
import { EMPTY, Observable, Subject, catchError, filter, takeUntil, throwError } from 'rxjs';

import { CreateGitHubConnectionRequest } from '../../../team/models/create-github-connection-request';
import { CreateSlackWebhookRequest } from '../../../team/models/create-slack-webhook-request';
import { CreateMicrosoftTeamsWebhookRequest } from '../../../team/models/create-microsoft-teams-webhook-request';
import { MicrosoftTeamsWebhookConnection } from '../../../team/models/microsoft-teams-webhook-connection';
import { TestMicrosoftTeamsWebhookResponse } from '../../../team/models/test-microsoft-teams-webhook-response';
import { microsoftTeamsWebhookUrl } from '../../../team/validators/microsoft-teams-webhook-url';

import { GitHubConnection, GitHubOwnerType } from '../../../team/models/github-connection';

import { GitHubConnectionTestResponse } from '../../../team/models/github-connection-test-response';

import { GitHubSyncResponse } from '../../../team/models/github-sync-response';
import { SlackWebhookConnection } from '../../../team/models/slack-webhook-connection';
import { TestSlackWebhookResponse } from '../../../team/models/test-slack-webhook-response';

import { GitHubApi } from '../../../team/services/github-api';
import { SlackApi } from '../../../team/services/slack-api';
import { MicrosoftTeamsApi } from '../../../team/services/microsoft-teams-api';

@Component({
  selector: 'app-integration-settings',

  standalone: true,

  imports: [
    TeamSelector,
    JiraIntegration,
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

  templateUrl: './integration-settings.html',

  styleUrl: './integration-settings.scss',

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IntegrationSettings {
  readonly i18n = inject(I18nService);
  readonly teamContext = inject(TeamContext);
  private readonly githubApi = inject(GitHubApi);
  private readonly slackApi = inject(SlackApi);
  private readonly microsoftTeamsApi = inject(MicrosoftTeamsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  private readonly contextChanged = new Subject<void>();
  private contextGeneration = 0;

  constructor() {
    effect(() => {
      this.teamContext.selectionVersion();
      const teamId = this.teamContext.selectedTeamId();
      this.contextGeneration++;
      this.contextChanged.next();
      this.resetGitHubState();
      this.resetSlackState();
      this.resetMicrosoftTeamsState();
      if (teamId) {
        this.loadGitHubConnection(teamId);
        this.loadSlackConnection(teamId);
        this.loadMicrosoftTeamsConnection(teamId);
      }
    });
  }

  private scopedRequest<T>() {
    const generation = this.contextGeneration;
    const selectionVersion = this.teamContext.selectionVersion();
    const teamId = this.teamContext.selectedTeamId();
    const isCurrent = () =>
      generation === this.contextGeneration &&
      selectionVersion === this.teamContext.selectionVersion() &&
      teamId === this.teamContext.selectedTeamId();

    return (source: Observable<T>): Observable<T> =>
      source.pipe(
        takeUntil(this.contextChanged),
        takeUntilDestroyed(this.destroyRef),
        filter(isCurrent),
        catchError((error: unknown) => (isCurrent() ? throwError(() => error) : EMPTY)),
      );
  }
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
    { value: 'User', label: 'team.ownerTypes.User' },

    { value: 'Organization', label: 'team.ownerTypes.Organization' },
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

  readonly slackConnection = signal<SlackWebhookConnection | null>(null);
  readonly slackConnectionLoading = signal(false);
  readonly slackConnectionError = signal(false);
  readonly slackConnecting = signal(false);
  readonly slackTesting = signal(false);
  readonly slackTestResult = signal<TestSlackWebhookResponse | null>(null);
  readonly slackDisconnecting = signal(false);
  readonly slackWebhookForm = new FormGroup({
    webhookUrl: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(2048),
        Validators.pattern(/^https:\/\/hooks\.slack\.com\/services\/.+$/),
      ],
    }),
  });

  readonly microsoftTeamsConnection = signal<MicrosoftTeamsWebhookConnection | null>(null);
  readonly microsoftTeamsConnectionLoading = signal(false);
  readonly microsoftTeamsConnectionError = signal(false);
  readonly microsoftTeamsConnecting = signal(false);
  readonly microsoftTeamsTesting = signal(false);
  readonly microsoftTeamsTestResult = signal<TestMicrosoftTeamsWebhookResponse | null>(null);
  readonly microsoftTeamsDisconnecting = signal(false);
  readonly microsoftTeamsWebhookForm = new FormGroup({
    webhookUrl: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(4096), microsoftTeamsWebhookUrl],
    }),
  });

  reloadGitHubConnection(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId) {
      return;
    }

    this.loadGitHubConnection(teamId);
  }

  openGitHubConnection(): void {
    if (!this.teamContext.hasSelectedTeam() || this.githubConnection()) {
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

      .pipe(this.scopedRequest())

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

    if (!teamId || !this.githubConnection() || this.githubTesting()) {
      return;
    }

    this.githubTesting.set(true);

    this.githubTestResult.set(null);

    this.githubApi

      .testConnection(teamId)

      .pipe(this.scopedRequest())

      .subscribe({
        next: (result) => {
          this.githubTestResult.set(result);

          this.githubTesting.set(false);

          if (result.success) {
            this.message.success(result.message || this.i18n.t('team.notifications.githubValid'));

            return;
          }

          this.message.warning(
            result.message || this.i18n.t('team.notifications.githubTestFailed'),
          );
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

    if (!teamId || !this.githubConnection() || this.githubSyncing()) {
      return;
    }

    this.githubSyncing.set(true);

    this.githubSyncResult.set(null);

    this.githubApi

      .sync(teamId)

      .pipe(this.scopedRequest())

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

          // Refresh the connection from the backend because

          // LastSyncAt is owned by the server.

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

    if (!teamId || !this.githubConnection() || this.githubDisconnecting()) {
      return;
    }

    this.githubDisconnecting.set(true);

    this.githubApi

      .deleteConnection(teamId)

      .pipe(this.scopedRequest())

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

  connectSlackWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || this.slackWebhookForm.invalid || this.slackConnecting()) {
      this.slackWebhookForm.markAllAsTouched();
      return;
    }

    const webhookUrl = this.slackWebhookForm.controls.webhookUrl.value.trim();
    if (!webhookUrl) {
      this.slackWebhookForm.markAllAsTouched();
      return;
    }

    const request: CreateSlackWebhookRequest = { webhookUrl };
    this.slackConnecting.set(true);
    this.slackApi
      .createConnection(teamId, request)
      .pipe(this.scopedRequest())
      .subscribe({
        next: (connection) => {
          this.slackConnection.set(connection);
          this.slackConnecting.set(false);
          this.slackWebhookForm.reset({ webhookUrl: '' });
          this.message.success(this.i18n.t('team.notifications.slackConnected'));
        },
        error: (error) => {
          console.error('Failed to connect Slack webhook', error);
          this.slackConnecting.set(false);
          this.message.error(this.i18n.t('team.notifications.slackConnectFailed'));
        },
      });
  }

  testSlackWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.slackConnection() || this.slackTesting()) {
      return;
    }

    this.slackTesting.set(true);
    this.slackTestResult.set(null);
    this.slackApi
      .testConnection(teamId)
      .pipe(this.scopedRequest())
      .subscribe({
        next: (result) => {
          this.slackTestResult.set(result);
          this.slackTesting.set(false);
          if (result.success) {
            this.message.success(this.i18n.t('team.notifications.slackTested'));
          } else {
            this.message.warning(this.i18n.t('team.notifications.slackTestFailed'));
          }
        },
        error: (error) => {
          console.error('Failed to test Slack webhook', error);
          this.slackTesting.set(false);
          this.message.error(this.i18n.t('team.notifications.slackTestError'));
        },
      });
  }

  disconnectSlackWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.slackConnection() || this.slackDisconnecting()) {
      return;
    }

    this.slackDisconnecting.set(true);
    this.slackApi
      .deleteConnection(teamId)
      .pipe(this.scopedRequest())
      .subscribe({
        next: () => {
          this.slackConnection.set(null);
          this.slackTestResult.set(null);
          this.slackDisconnecting.set(false);
          this.message.success(this.i18n.t('team.notifications.slackDisconnected'));
        },
        error: (error) => {
          console.error('Failed to disconnect Slack webhook', error);
          this.slackDisconnecting.set(false);
          this.message.error(this.i18n.t('team.notifications.slackDisconnectFailed'));
        },
      });
  }

  reloadSlackConnection(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (teamId) {
      this.loadSlackConnection(teamId);
    }
  }

  private loadSlackConnection(teamId: string): void {
    this.slackConnectionLoading.set(true);
    this.slackConnectionError.set(false);
    this.slackConnection.set(null);
    this.slackTestResult.set(null);
    this.slackApi
      .getConnection(teamId)
      .pipe(this.scopedRequest())
      .subscribe({
        next: (connection) => {
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }
          this.slackConnection.set(connection);
          this.slackConnectionLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          if (this.teamContext.selectedTeamId() !== teamId) {
            return;
          }
          this.slackConnectionLoading.set(false);
          if (error.status === 404) {
            return;
          }
          console.error('Failed to load Slack webhook', error);
          this.slackConnectionError.set(true);
        },
      });
  }

  private resetSlackState(): void {
    this.slackConnection.set(null);
    this.slackConnectionLoading.set(false);
    this.slackConnectionError.set(false);
    this.slackConnecting.set(false);
    this.slackTesting.set(false);
    this.slackTestResult.set(null);
    this.slackDisconnecting.set(false);
    this.slackWebhookForm.reset({ webhookUrl: '' });
  }

  connectMicrosoftTeamsWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || this.microsoftTeamsWebhookForm.invalid || this.microsoftTeamsConnecting()) {
      this.microsoftTeamsWebhookForm.markAllAsTouched();
      return;
    }

    const request: CreateMicrosoftTeamsWebhookRequest = {
      webhookUrl: this.microsoftTeamsWebhookForm.controls.webhookUrl.value.trim(),
    };
    this.microsoftTeamsConnecting.set(true);
    this.microsoftTeamsApi
      .createConnection(teamId, request)
      .pipe(this.scopedRequest())
      .subscribe({
        next: (connection) => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsConnection.set(connection);
          this.microsoftTeamsConnecting.set(false);
          this.microsoftTeamsWebhookForm.reset({ webhookUrl: '' });
          this.message.success(this.i18n.t('team.notifications.microsoftTeamsConnected'));
        },
        error: () => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsConnecting.set(false);
          this.message.error(this.i18n.t('team.notifications.microsoftTeamsConnectFailed'));
        },
      });
  }

  testMicrosoftTeamsWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.microsoftTeamsConnection() || this.microsoftTeamsTesting()) return;

    this.microsoftTeamsTesting.set(true);
    this.microsoftTeamsTestResult.set(null);
    this.microsoftTeamsApi
      .testConnection(teamId)
      .pipe(this.scopedRequest())
      .subscribe({
        next: (result) => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsTestResult.set(result);
          this.microsoftTeamsTesting.set(false);
          if (result.success) {
            this.message.success(this.i18n.t('team.notifications.microsoftTeamsTested'));
          } else {
            this.message.warning(this.i18n.t('team.notifications.microsoftTeamsTestFailed'));
          }
        },
        error: () => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsTesting.set(false);
          this.message.error(this.i18n.t('team.notifications.microsoftTeamsTestError'));
        },
      });
  }

  disconnectMicrosoftTeamsWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.microsoftTeamsConnection() || this.microsoftTeamsDisconnecting()) return;

    this.microsoftTeamsDisconnecting.set(true);
    this.microsoftTeamsApi
      .deleteConnection(teamId)
      .pipe(this.scopedRequest())
      .subscribe({
        next: () => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsConnection.set(null);
          this.microsoftTeamsTestResult.set(null);
          this.microsoftTeamsDisconnecting.set(false);
          this.message.success(this.i18n.t('team.notifications.microsoftTeamsDisconnected'));
        },
        error: () => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsDisconnecting.set(false);
          this.message.error(this.i18n.t('team.notifications.microsoftTeamsDisconnectFailed'));
        },
      });
  }

  reloadMicrosoftTeamsConnection(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (teamId) this.loadMicrosoftTeamsConnection(teamId);
  }

  private loadMicrosoftTeamsConnection(teamId: string): void {
    this.microsoftTeamsConnectionLoading.set(true);
    this.microsoftTeamsConnectionError.set(false);
    this.microsoftTeamsConnection.set(null);
    this.microsoftTeamsTestResult.set(null);
    this.microsoftTeamsApi
      .getConnection(teamId)
      .pipe(this.scopedRequest())
      .subscribe({
        next: (connection) => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsConnection.set(connection);
          this.microsoftTeamsConnectionLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          if (this.teamContext.selectedTeamId() !== teamId) return;
          this.microsoftTeamsConnectionLoading.set(false);
          if (error.status !== 404) this.microsoftTeamsConnectionError.set(true);
        },
      });
  }

  private resetMicrosoftTeamsState(): void {
    this.microsoftTeamsConnection.set(null);
    this.microsoftTeamsConnectionLoading.set(false);
    this.microsoftTeamsConnectionError.set(false);
    this.microsoftTeamsConnecting.set(false);
    this.microsoftTeamsTesting.set(false);
    this.microsoftTeamsTestResult.set(null);
    this.microsoftTeamsDisconnecting.set(false);
    this.microsoftTeamsWebhookForm.reset({ webhookUrl: '' });
  }

  private loadGitHubConnection(teamId: string): void {
    this.githubConnectionLoading.set(true);

    this.githubConnectionError.set(false);

    this.githubConnection.set(null);

    this.githubTestResult.set(null);

    this.githubSyncResult.set(null);

    this.githubApi

      .getConnection(teamId)

      .pipe(this.scopedRequest())

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

      .pipe(this.scopedRequest())

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
}
