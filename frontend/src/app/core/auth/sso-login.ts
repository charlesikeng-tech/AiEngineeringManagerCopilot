import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { finalize } from 'rxjs';
import { Auth } from './auth';
import { PublicSsoApi, PublicSsoProvider } from './public-sso-api';

@Component({
  selector: 'app-sso-login',
  imports: [RouterLink, TranslatePipe, NzAlertModule, NzButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="access">
      <h1>{{ 'sso.login.title' | translate }}</h1>
      <p>{{ 'sso.login.policy' | translate }}</p>
      @if (error()) { <nz-alert nzType="error" [nzMessage]="('sso.login.errors.' + error()) | translate" nzShowIcon /> }
      @if (loading()) { <p role="status">{{ 'sso.login.loading' | translate }}</p> }
      @else if (!providers().length) { <p>{{ 'sso.login.empty' | translate }}</p> }
      @for (provider of providers(); track provider.id) {
        <button nz-button nzType="primary" type="button" [nzLoading]="busy() === provider.id"
          [disabled]="busy() !== null" (click)="login(provider.id)">
          {{ 'sso.login.continue' | translate }} {{ provider.name }} ({{ provider.type }})
        </button>
      }
      <p><a routerLink="/admin/login">{{ 'sso.login.recovery' | translate }}</a></p>
    </main>
  `,
  styles: [`.access { max-width: 480px; margin: 8vh auto; padding: 24px; } button { display: block; margin: 16px 0; }`],
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

  ngOnInit() {
    const failure = this.route.snapshot.queryParamMap.get('ssoError');
    if (this.auth.user() && !failure) { void this.router.navigateByUrl('/dashboard'); return; }
    if (failure) this.error.set(failure === 'email_collision' ? failure : 'login_failed');
    this.api.providers().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (providers) => this.providers.set(providers),
      error: () => this.error.set('request_failed'),
    });
  }

  login(id: string) {
    if (this.busy() !== null) return;
    this.busy.set(id);
    this.error.set('');
    this.api.start(id).subscribe({
      next: ({ authorizationUrl }) => {
        try { this.api.navigate(authorizationUrl); }
        catch { this.busy.set(null); this.error.set('login_failed'); }
      },
      error: (error: HttpErrorResponse) => {
        this.busy.set(null);
        this.error.set(error.status === 429 ? 'rate_limited' : 'login_failed');
      },
    });
  }
}
