import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { timeout } from 'rxjs';

export type ProviderType = 'Auth0' | 'Okta' | 'EntraId';
export interface ProviderConfiguration {
  type: ProviderType;
  name: string;
  authority: string;
  clientId: string;
  hasSecret: boolean;
}
export interface SsoProvider extends ProviderConfiguration {
  id: string;
  revision: number;
  testedRevision: number | null;
  testedAt: string | null;
  lastTestError: string | null;
  activeRevision: number | null;
  active: ProviderConfiguration | null;
}
export interface SsoDraft {
  revision: number | null;
  type: ProviderType;
  name: string;
  tenant: string;
  authorizationServer: string;
  clientId: string;
  secretAction: 'retain' | 'replace' | 'clear';
  clientSecret?: string;
}

@Injectable({ providedIn: 'root' })
export class SsoApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/auth/sso/providers`;

  list() {
    return this.http.get<{ callbackUrl: string; providers: SsoProvider[] }>(this.url).pipe(timeout(15000));
  }
  save(id: string | null, draft: SsoDraft) {
    return (id
      ? this.http.put<SsoProvider>(`${this.url}/${id}`, draft)
      : this.http.post<SsoProvider>(this.url, draft)).pipe(timeout(15000));
  }
  test(provider: SsoProvider) {
    return this.http.post<{ authorizationUrl: string }>(`${this.url}/${provider.id}/test`,
      { revision: provider.revision }).pipe(timeout(60000));
  }
  activate(provider: SsoProvider) {
    return this.http.post<SsoProvider>(`${this.url}/${provider.id}/activate`,
      { revision: provider.revision }).pipe(timeout(15000));
  }
  deactivate(provider: SsoProvider) {
    return this.http.post<SsoProvider>(`${this.url}/${provider.id}/deactivate`, {}).pipe(timeout(15000));
  }
  delete(provider: SsoProvider) {
    return this.http.delete<void>(`${this.url}/${provider.id}`).pipe(timeout(15000));
  }
}
