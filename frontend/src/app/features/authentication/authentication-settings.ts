import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { finalize, Observable } from 'rxjs';

import { SsoApi, SsoDraft, SsoProvider } from './sso-api';

@Component({
  selector: 'app-authentication-settings',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    NzAlertModule,
    NzButtonModule,
    NzEmptyModule,
    NzIconModule,
    NzInputModule,
    NzSelectModule,
    NzSpinModule,
  ],
  templateUrl: './authentication-settings.html',
  styleUrl: './authentication-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthenticationSettings {
  private readonly api = inject(SsoApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly providers = signal<SsoProvider[]>([]);
  readonly callbackUrl = signal('');
  readonly pending = signal(false);
  readonly loaded = signal(false);
  readonly error = signal('');
  readonly feedback = signal('');
  readonly editing = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly hasSecret = signal(false);

  draft: SsoDraft = this.emptyDraft();
  clientSecret = '';

  constructor() {
    const outcome = this.route.snapshot.queryParamMap.get('ssoTest');

    if (outcome) {
      if (outcome === 'success') {
        this.feedback.set('sso.testSuccess');
      } else {
        this.error.set('sso.errors.connection_failed');
      }

      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true,
      });
    }

    this.load(true);
  }

  load(preserveFeedback = false): void {
    this.run(
      this.api.list(),
      (result) => {
        this.providers.set(result.providers);
        this.callbackUrl.set(result.callbackUrl);
        this.loaded.set(true);
      },
      !preserveFeedback,
    );
  }

  edit(provider?: SsoProvider): void {
    this.error.set('');
    this.feedback.set('');

    this.editingId.set(provider?.id ?? null);
    this.hasSecret.set(provider?.hasSecret ?? false);

    this.clientSecret = '';

    this.draft = provider
      ? {
          revision: provider.revision,
          type: provider.type,
          name: provider.name,

          tenant:
            provider.type === 'EntraId'
              ? new URL(provider.authority).pathname.split('/')[1]
              : new URL(provider.authority).hostname,

          authorizationServer:
            provider.type === 'Okta'
              ? new URL(provider.authority).pathname.split('/')[2]
              : 'default',

          clientId: provider.clientId,
          secretAction: 'retain',
        }
      : this.emptyDraft();

    this.editing.set(true);
  }

  cancel(): void {
    this.clientSecret = '';
    this.editing.set(false);
  }

  save(): void {
    if (this.pending()) {
      return;
    }

    const draft = {
      ...this.draft,
    };

    if (draft.secretAction === 'replace') {
      draft.clientSecret = this.clientSecret;
    }

    /*
     * Credentials must never remain in the component
     * after the request has been initiated.
     */
    this.clientSecret = '';

    this.run(this.api.save(this.editingId(), draft), (provider) => {
      this.upsert(provider);
      this.cancel();

      this.feedback.set('sso.saved');
    });
  }

  test(provider: SsoProvider): void {
    this.run(this.api.test(provider), ({ authorizationUrl }) => {
      window.location.assign(authorizationUrl);
    });
  }

  activate(provider: SsoProvider): void {
    this.run(this.api.activate(provider), (saved) => {
      this.upsert(saved);
      this.feedback.set('sso.activated');
    });
  }

  deactivate(provider: SsoProvider): void {
    this.run(this.api.deactivate(provider), (saved) => {
      this.upsert(saved);
      this.feedback.set('sso.deactivated');
    });
  }

  delete(provider: SsoProvider): void {
    this.run(this.api.delete(provider), () => {
      this.providers.update((rows) => rows.filter((row) => row.id !== provider.id));
    });
  }

  private run<T>(request: Observable<T>, success: (result: T) => void, clearMessages = true): void {
    if (this.pending()) {
      return;
    }

    this.pending.set(true);

    if (clearMessages) {
      this.error.set('');
      this.feedback.set('');
    }

    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.pending.set(false);
        }),
      )
      .subscribe({
        next: success,

        error: (error: HttpErrorResponse) => {
          const known = [
            'invalid_provider',
            'revision_conflict',
            'test_required',
            'deactivate_required',
            'connection_failed',
            'deployment_configuration',
          ];

          const code = error.error?.error;

          this.error.set(`sso.errors.${known.includes(code) ? code : 'request_failed'}`);
        },
      });
  }

  private upsert(provider: SsoProvider): void {
    this.providers.update((rows) => [...rows.filter((row) => row.id !== provider.id), provider]);
  }

  private emptyDraft(): SsoDraft {
    return {
      revision: null,
      type: 'Auth0',
      name: '',
      tenant: '',
      authorizationServer: 'default',
      clientId: '',
      secretAction: 'retain',
    };
  }
}
