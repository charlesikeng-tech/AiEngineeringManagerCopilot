import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe } from '@core/i18n/localized-format.pipes';
import { TeamContext } from '@core/team/team-context';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { Observable, Subject, takeUntil } from 'rxjs';
import {
  JiraConnection,
  JiraSyncResult,
  TestJiraConnectionResponse,
} from '../../models/jira-connection';
import { JiraApi } from '../../services/jira-api';

@Component({
  selector: 'app-jira-integration',
  standalone: true,
  imports: [
    TranslatePipe,
    LocalizedDatePipe,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzInputModule,
    NzPopconfirmModule,
    NzSpinModule,
  ],
  templateUrl: './jira-integration.html',
  styleUrl: './jira-integration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JiraIntegration {
  private readonly api = inject(JiraApi);
  private readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
  private readonly contextChanged = new Subject<void>();
  private contextVersion = 0;

  readonly connection = signal<JiraConnection | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly connectOpen = signal(false);
  readonly saving = signal(false);
  readonly testing = signal(false);
  readonly syncing = signal(false);
  readonly disconnecting = signal(false);
  readonly testResult = signal<TestJiraConnectionResponse | null>(null);
  readonly syncResult = signal<JiraSyncResult | null>(null);
  readonly busy = () =>
    this.loading() || this.saving() || this.testing() || this.syncing() || this.disconnecting();

  readonly form = new FormGroup({
    baseUrl: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(2048),
        Validators.pattern(/^https:\/\/[a-z0-9][a-z0-9.-]*(?::\d+)?\/?$/i),
      ],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(320)],
    }),
    apiToken: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/\S/)],
    }),
    projectKey: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[a-z][a-z0-9_]*$/i)],
    }),
  });

  constructor() {
    toObservable(this.teamContext.selectionVersion)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const teamId = this.teamContext.selectedTeamId();
        this.reset();
        if (teamId) {
          this.loadConnection(teamId);
        }
      });
  }

  openConnection(): void {
    this.form.reset();
    this.connectOpen.set(true);
  }

  closeConnection(): void {
    if (this.saving()) {
      return;
    }
    this.form.reset();
    this.connectOpen.set(false);
  }

  connect(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || this.busy()) {
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this.subscribe(
      this.api.createConnection(teamId, {
        baseUrl: value.baseUrl.trim(),
        email: value.email.trim(),
        apiToken: value.apiToken.trim(),
        projectKey: value.projectKey.trim().toUpperCase(),
      }),
      (connection) => {
        this.connection.set(connection);
        this.saving.set(false);
        this.closeConnection();
        this.message.success(this.i18n.t('integrations.jira.connectedMessage'));
      },
      () => {
        this.saving.set(false);
        this.message.error(this.i18n.t('integrations.jira.connectFailed'));
      },
    );
  }

  testConnection(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.connection() || this.busy()) {
      return;
    }
    this.testing.set(true);
    this.testResult.set(null);
    this.subscribe(
      this.api.testConnection(teamId),
      (result) => {
        this.testing.set(false);
        this.testResult.set(result);
        if (result.isValid) {
          this.message.success(this.i18n.t('integrations.jira.testSucceeded'));
        } else {
          this.message.warning(this.i18n.t('integrations.jira.testFailed'));
        }
      },
      () => {
        this.testing.set(false);
        this.message.error(this.i18n.t('integrations.jira.testFailed'));
      },
    );
  }

  sync(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.connection() || this.busy()) {
      return;
    }
    this.syncing.set(true);
    this.syncResult.set(null);
    this.subscribe(
      this.api.sync(teamId),
      (result) => {
        this.syncing.set(false);
        this.syncResult.set(result);
        this.message.success(this.i18n.t('integrations.jira.syncSucceeded'));
        this.loadConnection(teamId);
      },
      () => {
        this.syncing.set(false);
        this.message.error(this.i18n.t('integrations.jira.syncFailed'));
      },
    );
  }

  disconnect(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (!teamId || !this.connection() || this.busy()) {
      return;
    }
    this.disconnecting.set(true);
    this.subscribe(
      this.api.deleteConnection(teamId),
      () => {
        this.connection.set(null);
        this.testResult.set(null);
        this.syncResult.set(null);
        this.disconnecting.set(false);
        this.form.reset();
        this.message.success(this.i18n.t('integrations.jira.disconnectedMessage'));
      },
      () => {
        this.disconnecting.set(false);
        this.message.error(this.i18n.t('integrations.jira.disconnectFailed'));
      },
    );
  }

  reload(): void {
    const teamId = this.teamContext.selectedTeamId();
    if (teamId && !this.busy()) {
      this.loadConnection(teamId);
    }
  }

  private loadConnection(teamId: string): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.subscribe(
      this.api.getConnection(teamId),
      (connection) => {
        this.connection.set(connection);
        this.loading.set(false);
      },
      (error) => {
        this.loading.set(false);
        this.loadError.set(error.status !== 404);
        if (error.status === 404) {
          this.connection.set(null);
        }
      },
    );
  }

  private subscribe<T>(
    request: Observable<T>,
    next: (result: T) => void,
    error: (response: HttpErrorResponse) => void,
  ): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.contextVersion;
    const selectionVersion = this.teamContext.selectionVersion();
    const isCurrent = () =>
      version === this.contextVersion &&
      selectionVersion === this.teamContext.selectionVersion() &&
      teamId === this.teamContext.selectedTeamId();
    request.pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (isCurrent()) {
          next(result);
        }
      },
      error: (response: HttpErrorResponse) => {
        if (isCurrent()) {
          error(response);
        }
      },
    });
  }

  private reset(): void {
    this.contextVersion++;
    this.contextChanged.next();
    this.connection.set(null);
    this.loading.set(false);
    this.loadError.set(false);
    this.connectOpen.set(false);
    this.saving.set(false);
    this.testing.set(false);
    this.syncing.set(false);
    this.disconnecting.set(false);
    this.testResult.set(null);
    this.syncResult.set(null);
    this.form.reset();
  }
}
