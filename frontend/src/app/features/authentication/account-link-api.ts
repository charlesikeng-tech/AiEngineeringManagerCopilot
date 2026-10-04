import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { timeout } from 'rxjs';
import { PublicSsoApi } from '@core/auth/public-sso-api';

export interface LinkedIdentity {
  id: string;
  issuer: string;
  providerName: string | null;
  administratorAccessApproved: boolean;
}
export interface AccountSecurityState {
  canLink: boolean;
  identities: LinkedIdentity[];
}

@Injectable({ providedIn: 'root' })
export class AccountLinkApi {
  private readonly http = inject(HttpClient);
  private readonly publicSso = inject(PublicSsoApi);
  private readonly root = `${environment.apiUrl}/auth/account/identities`;

  list() {
    return this.http.get<AccountSecurityState>(this.root).pipe(timeout(10000));
  }
  providers() { return this.publicSso.providers(); }
  start(providerId: string, password: string, approveAdministratorAccess: boolean) {
    return this.http.post<{ authorizationUrl: string }>(`${this.root}/start`,
      { providerId, password, approveAdministratorAccess }).pipe(timeout(15000));
  }
  unlink(id: string, password: string) {
    return this.http.post<void>(`${this.root}/${encodeURIComponent(id)}/unlink`, { password }).pipe(timeout(15000));
  }
  navigate(url: string) { this.publicSso.navigate(url); }
}
