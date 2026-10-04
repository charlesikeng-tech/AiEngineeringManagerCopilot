import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { finalize, forkJoin } from 'rxjs';

import { Auth } from '@core/auth/auth';
import { PublicSsoProvider } from '@features/authentication/data-access/public-sso-api';

import { AccountLinkApi, AccountSecurityState } from '../../data-access/account-link-api';

@Component({
  selector: 'app-account-security',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    TranslatePipe,
    NzAlertModule,
    NzButtonModule,
    NzEmptyModule,
    NzIconModule,
    NzInputModule,
    NzSelectModule,
    NzSpinModule,
  ],
  templateUrl: './account-security.html',
  styleUrl: './account-security.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountSecurity implements OnInit {
  private readonly api = inject(AccountLinkApi);
  private readonly auth = inject(Auth);
  private readonly route = inject(ActivatedRoute);

  readonly administrator = () => this.auth.user()?.role === 'PlatformAdministrator';

  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly outcome = signal('');

  readonly state = signal<AccountSecurityState | null>(null);

  readonly providers = signal<PublicSsoProvider[]>([]);

  providerId = '';
  password = '';
  consent = false;

  ngOnInit(): void {
    const outcome = this.route.snapshot.queryParamMap.get('link');

    if (outcome) {
      this.outcome.set(
        ['success', 'identity_conflict'].includes(outcome) ? outcome : 'link_failed',
      );
    }

    this.load();
  }

  load(): void {
    if (!this.administrator()) {
      return;
    }

    this.loading.set(true);
    this.error.set('');

    forkJoin({
      state: this.api.list(),
      providers: this.api.providers(),
    })
      .pipe(
        finalize(() => {
          this.loading.set(false);
        }),
      )
      .subscribe({
        next: ({ state, providers }) => {
          this.state.set(state);
          this.providers.set(providers);
        },
        error: () => {
          this.state.set(null);
          this.error.set('request_failed');
        },
      });
  }

  start(): void {
    if (
      this.busy() ||
      !this.state()?.canLink ||
      !this.providerId ||
      !this.password ||
      !this.consent
    ) {
      return;
    }

    this.busy.set(true);
    this.error.set('');

    const password = this.password;

    /*
     * Do not keep the local administrator password
     * longer than necessary in the component state.
     */
    this.password = '';

    this.api.start(this.providerId, password, this.consent).subscribe({
      next: ({ authorizationUrl }) => {
        try {
          this.api.navigate(authorizationUrl);
        } catch {
          this.busy.set(false);
          this.error.set('request_failed');
        }
      },
      error: (error: HttpErrorResponse) => {
        this.failed(error);
      },
    });
  }

  unlink(id: string): void {
    if (this.busy() || !this.state()?.canLink || !this.password) {
      return;
    }

    this.busy.set(true);
    this.error.set('');

    const password = this.password;

    /*
     * Same security rule as linking:
     * never retain the password after use.
     */
    this.password = '';

    this.api.unlink(id, password).subscribe({
      next: () => {
        this.busy.set(false);
        this.outcome.set('unlinked');

        this.load();
      },
      error: (error: HttpErrorResponse) => {
        this.failed(error);
      },
    });
  }

  private failed(error: HttpErrorResponse): void {
    this.busy.set(false);

    this.error.set(
      error.status === 429
        ? 'rate_limited'
        : error.status === 401
          ? 'confirmation_failed'
          : 'request_failed',
    );
  }
}
