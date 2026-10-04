import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { finalize } from 'rxjs';

import { Auth } from './auth';
import { PublicSsoApi, PublicSsoProvider } from './public-sso-api';

@Component({
  selector: 'app-sso-login',
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    NzAlertModule,
    NzButtonModule,
    NzEmptyModule,
    NzIconModule,
    NzSpinModule,
  ],
  templateUrl: './sso-login.html',
  styleUrl: './sso-login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SsoLogin implements OnInit {
  private readonly api = inject(PublicSsoApi);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly providers = signal<PublicSsoProvider[]>([]);
  readonly loading = signal(true);
  readonly busy = signal<string | null>(null);
  readonly error = signal('');
  readonly diagnosticId = signal('');

  ngOnInit(): void {
    const failure = this.route.snapshot.queryParamMap.get('ssoError');

    if (this.auth.user() && !failure) {
      void this.router.navigateByUrl('/dashboard');
      return;
    }

    if (failure) {
      this.error.set(failure === 'email_collision' ? failure : 'login_failed');
    }

    this.api
      .providers()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        }),
      )
      .subscribe({
        next: (providers) => {
          this.providers.set(providers);
        },

        error: () => {
          this.error.set('request_failed');
        },
      });
  }

  login(id: string): void {
    if (this.busy() !== null) {
      return;
    }

    this.busy.set(id);
    this.error.set('');
    this.diagnosticId.set('');

    this.api.start(id).subscribe({
      next: ({ authorizationUrl }) => {
        try {
          this.api.navigate(authorizationUrl);
        } catch {
          this.busy.set(null);
          this.error.set('login_failed');
        }
      },

      error: (error: HttpErrorResponse) => {
        this.busy.set(null);

        this.error.set(error.status === 429 ? 'rate_limited' : 'login_failed');

        const diagnosticId = error.error?.diagnosticId;

        if (typeof diagnosticId === 'string' && /^[a-f0-9]{32}$/.test(diagnosticId)) {
          this.diagnosticId.set(diagnosticId);
        }
      },
    });
  }
}
