import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize, forkJoin } from 'rxjs';
import { Auth } from '@core/auth/auth';
import { PublicSsoProvider } from '@core/auth/public-sso-api';
import { AccountLinkApi, AccountSecurityState } from './account-link-api';

@Component({
  selector: 'app-account-security',
  imports: [FormsModule, RouterLink, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="security">
      <h1>{{ 'accountLink.title' | translate }}</h1>
      @if (!administrator()) {
        <p>{{ 'accountLink.unsupported' | translate }}</p>
      } @else {
        <p>{{ 'accountLink.explanation' | translate }}</p>
        @if (outcome()) { <p role="status">{{ ('accountLink.outcomes.' + outcome()) | translate }}</p> }
        @if (error()) { <p role="alert">{{ ('accountLink.errors.' + error()) | translate }}</p> }
        @if (loading()) { <p role="status">{{ 'accountLink.loading' | translate }}</p> }
        @if (state(); as account) {
          <h2>{{ 'accountLink.identities' | translate }}</h2>
          @if (!account.identities.length) { <p>{{ 'accountLink.empty' | translate }}</p> }
          <ul>
            @for (identity of account.identities; track identity.id) {
              <li>
                {{ identity.providerName || identity.issuer }} — {{ identity.issuer }}
                @if (identity.administratorAccessApproved) { <span>{{ 'accountLink.approved' | translate }}</span> }
                @if (account.canLink) {
                  <button type="button" [disabled]="busy() || !password" (click)="unlink(identity.id)">
                    {{ 'accountLink.unlink' | translate }}
                  </button>
                }
              </li>
            }
          </ul>
          @if (!account.canLink) {
            <p>{{ 'accountLink.localRequired' | translate }}</p>
            <a routerLink="/admin/login">{{ 'accountLink.localLogin' | translate }}</a>
          } @else {
            <form (ngSubmit)="start()">
              <label for="provider">{{ 'accountLink.provider' | translate }}</label>
              <select id="provider" name="provider" [(ngModel)]="providerId" [disabled]="busy()">
                <option value="">{{ 'accountLink.select' | translate }}</option>
                @for (provider of providers(); track provider.id) {
                  <option [value]="provider.id">{{ provider.name }} ({{ provider.type }})</option>
                }
              </select>
              @if (!providers().length) { <p>{{ 'accountLink.noProviders' | translate }}</p> }
              <label for="password">{{ 'accountLink.password' | translate }}</label>
              <input id="password" name="password" type="password" autocomplete="current-password"
                maxlength="128" [(ngModel)]="password" [disabled]="busy()" />
              <label><input name="consent" type="checkbox" [(ngModel)]="consent" [disabled]="busy()" />
                {{ 'accountLink.consent' | translate }}
              </label>
              <button type="submit" [disabled]="busy() || !providerId || !password || !consent">
                {{ (busy() ? 'accountLink.pending' : 'accountLink.start') | translate }}
              </button>
              <p>{{ 'accountLink.unlinkExplanation' | translate }}</p>
            </form>
          }
        }
      }
    </section>
  `,
  styles: [`.security { max-width: 760px; } form { display: grid; gap: 12px; } li { margin-bottom: 12px; }`],
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

  ngOnInit() {
    const outcome = this.route.snapshot.queryParamMap.get('link');
    if (outcome) this.outcome.set(['success', 'identity_conflict'].includes(outcome) ? outcome : 'link_failed');
    this.load();
  }

  load() {
    if (!this.administrator()) return;
    this.loading.set(true);
    forkJoin({ state: this.api.list(), providers: this.api.providers() })
      .pipe(finalize(() => this.loading.set(false))).subscribe({
        next: ({ state, providers }) => { this.state.set(state); this.providers.set(providers); },
        error: () => { this.state.set(null); this.error.set('request_failed'); },
      });
  }

  start() {
    if (this.busy() || !this.state()?.canLink || !this.providerId || !this.password || !this.consent) return;
    this.busy.set(true);
    this.error.set('');
    const password = this.password;
    this.password = '';
    this.api.start(this.providerId, password, this.consent).subscribe({
      next: ({ authorizationUrl }) => {
        try { this.api.navigate(authorizationUrl); }
        catch { this.busy.set(false); this.error.set('request_failed'); }
      },
      error: (error: HttpErrorResponse) => this.failed(error),
    });
  }

  unlink(id: string) {
    if (this.busy() || !this.state()?.canLink || !this.password) return;
    this.busy.set(true);
    this.error.set('');
    const password = this.password;
    this.password = '';
    this.api.unlink(id, password).subscribe({
      next: () => { this.busy.set(false); this.outcome.set('unlinked'); this.load(); },
      error: (error: HttpErrorResponse) => this.failed(error),
    });
  }

  private failed(error: HttpErrorResponse) {
    this.busy.set(false);
    this.error.set(error.status === 429 ? 'rate_limited' : error.status === 401 ? 'confirmation_failed' : 'request_failed');
  }
}
