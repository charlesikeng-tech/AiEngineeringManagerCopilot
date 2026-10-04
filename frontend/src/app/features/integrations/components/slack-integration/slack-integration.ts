import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { I18nService } from '@core/i18n/i18n.service';
import { TranslatePipe } from '@ngx-translate/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { SlackApi } from '../../providers/slack/data-access/slack-api';
import { SlackWebhookConnection } from '../../providers/slack/models/slack-webhook-connection';
import { TestSlackWebhookResponse } from '../../providers/slack/models/test-slack-webhook-response';
import { IntegrationRequestScope } from '../../services/integration-request-scope';

@Component({
  selector: 'app-slack-integration',
  imports: [
    TranslatePipe,
    ReactiveFormsModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzPopconfirmModule,
    NzSpinModule,
  ],
  providers: [IntegrationRequestScope],
  templateUrl: './slack-integration.html',
  styleUrl: './slack-integration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlackIntegration {
  private readonly api = inject(SlackApi);
  private readonly scope = inject(IntegrationRequestScope);
  private readonly teamContext = this.scope.teamContext;
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
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

  constructor() {
    this.scope.watch(
      () => this.reset(),
      (teamId) => this.loadSlackConnection(teamId),
    );
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
    this.slackConnecting.set(true);
    this.api
      .createConnection(teamId, { webhookUrl })
      .pipe(this.scope.request())
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
    if (!teamId || !this.slackConnection() || this.slackTesting()) return;
    this.slackTesting.set(true);
    this.slackTestResult.set(null);
    this.api
      .testConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (result) => {
          this.slackTestResult.set(result);
          this.slackTesting.set(false);
          if (result.success) this.message.success(this.i18n.t('team.notifications.slackTested'));
          else this.message.warning(this.i18n.t('team.notifications.slackTestFailed'));
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
    if (!teamId || !this.slackConnection() || this.slackDisconnecting()) return;
    this.slackDisconnecting.set(true);
    this.api
      .deleteConnection(teamId)
      .pipe(this.scope.request())
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
    if (teamId) this.loadSlackConnection(teamId);
  }

  private loadSlackConnection(teamId: string): void {
    this.slackConnectionLoading.set(true);
    this.slackConnectionError.set(false);
    this.slackConnection.set(null);
    this.slackTestResult.set(null);
    this.api
      .getConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (connection) => {
          this.slackConnection.set(connection);
          this.slackConnectionLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.slackConnectionLoading.set(false);
          if (error.status === 404) return;
          console.error('Failed to load Slack webhook', error);
          this.slackConnectionError.set(true);
        },
      });
  }

  private reset(): void {
    this.slackConnection.set(null);
    this.slackConnectionLoading.set(false);
    this.slackConnectionError.set(false);
    this.slackConnecting.set(false);
    this.slackTesting.set(false);
    this.slackTestResult.set(null);
    this.slackDisconnecting.set(false);
    this.slackWebhookForm.reset({ webhookUrl: '' });
  }
}
