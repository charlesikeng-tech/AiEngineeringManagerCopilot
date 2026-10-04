import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { timeout } from 'rxjs';

export interface PublicSsoProvider {
  id: string;
  name: string;
  type: 'Auth0' | 'Okta' | 'EntraId';
}

@Injectable({ providedIn: 'root' })
export class PublicSsoApi {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);

  providers() {
    return this.http.get<PublicSsoProvider[]>(`${environment.apiUrl}/auth/sso/login/providers`).pipe(timeout(10000));
  }

  start(id: string) {
    return this.http.post<{ authorizationUrl: string }>(
      `${environment.apiUrl}/auth/sso/login/${encodeURIComponent(id)}/start`, {},
    ).pipe(timeout(15000));
  }

  navigate(authorizationUrl: string) {
    const target = new URL(authorizationUrl);
    if (target.protocol !== 'https:' || target.username || target.password) throw new Error('Invalid authorization URL');
    this.document.defaultView?.location.assign(target.href);
  }
}
