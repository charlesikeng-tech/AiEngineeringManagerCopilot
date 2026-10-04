import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { I18nService } from '@core/i18n/i18n.service';
import { TranslatePipe } from '@ngx-translate/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { MicrosoftTeamsApi } from '../../providers/microsoft-teams/data-access/microsoft-teams-api';
import { MicrosoftTeamsWebhookConnection } from '../../providers/microsoft-teams/models/microsoft-teams-webhook-connection';
import { TestMicrosoftTeamsWebhookResponse } from '../../providers/microsoft-teams/models/test-microsoft-teams-webhook-response';
import { microsoftTeamsWebhookUrl } from '../../providers/microsoft-teams/validators/microsoft-teams-webhook-url';
import { IntegrationRequestScope } from '../../services/integration-request-scope';

@Component({
  selector: 'app-microsoft-teams-integration',
  imports: [
    TranslatePipe,
    ReactiveFormsModule,
    NzButtonModule,
    NzInputModule,
    NzPopconfirmModule,
    NzSpinModule,
  ],
  providers: [IntegrationRequestScope],
  templateUrl: './microsoft-teams-integration.html',
  styleUrl: './microsoft-teams-integration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicrosoftTeamsIntegration {
  private readonly api = inject(MicrosoftTeamsApi);
  private readonly scope = inject(IntegrationRequestScope);
  private readonly teamContext = this.scope.teamContext;
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
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

  constructor() {
    this.scope.watch(
      () => this.reset(),
      (teamId) => this.loadMicrosoftTeamsConnection(teamId),
    );
  }

  connectMicrosoftTeamsWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || this.microsoftTeamsWebhookForm.invalid || this.microsoftTeamsConnecting()) {
      this.microsoftTeamsWebhookForm.markAllAsTouched();
      return;
    }
    const webhookUrl = this.microsoftTeamsWebhookForm.controls.webhookUrl.value.trim();
    this.microsoftTeamsConnecting.set(true);
    this.api
      .createConnection(teamId, { webhookUrl })
      .pipe(this.scope.request())
      .subscribe({
        next: (connection) => {
          this.microsoftTeamsConnection.set(connection);
          this.microsoftTeamsConnecting.set(false);
          this.microsoftTeamsWebhookForm.reset({ webhookUrl: '' });
          this.message.success(this.i18n.t('team.notifications.microsoftTeamsConnected'));
        },
        error: () => {
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
    this.api
      .testConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (result) => {
          this.microsoftTeamsTestResult.set(result);
          this.microsoftTeamsTesting.set(false);
          if (result.success)
            this.message.success(this.i18n.t('team.notifications.microsoftTeamsTested'));
          else this.message.warning(this.i18n.t('team.notifications.microsoftTeamsTestFailed'));
        },
        error: () => {
          this.microsoftTeamsTesting.set(false);
          this.message.error(this.i18n.t('team.notifications.microsoftTeamsTestError'));
        },
      });
  }

  disconnectMicrosoftTeamsWebhook(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.microsoftTeamsConnection() || this.microsoftTeamsDisconnecting()) return;
    this.microsoftTeamsDisconnecting.set(true);
    this.api
      .deleteConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: () => {
          this.microsoftTeamsConnection.set(null);
          this.microsoftTeamsTestResult.set(null);
          this.microsoftTeamsDisconnecting.set(false);
          this.message.success(this.i18n.t('team.notifications.microsoftTeamsDisconnected'));
        },
        error: () => {
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
    this.api
      .getConnection(teamId)
      .pipe(this.scope.request())
      .subscribe({
        next: (connection) => {
          this.microsoftTeamsConnection.set(connection);
          this.microsoftTeamsConnectionLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.microsoftTeamsConnectionLoading.set(false);
          if (error.status !== 404) this.microsoftTeamsConnectionError.set(true);
        },
      });
  }

  private reset(): void {
    this.microsoftTeamsConnection.set(null);
    this.microsoftTeamsConnectionLoading.set(false);
    this.microsoftTeamsConnectionError.set(false);
    this.microsoftTeamsConnecting.set(false);
    this.microsoftTeamsTesting.set(false);
    this.microsoftTeamsTestResult.set(null);
    this.microsoftTeamsDisconnecting.set(false);
    this.microsoftTeamsWebhookForm.reset({ webhookUrl: '' });
  }
}
