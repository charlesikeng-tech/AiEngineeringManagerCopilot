import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';

import { environment } from '@environments/environment';

interface DevelopmentTokenResponse {
  accessToken: string;
  userId: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly tokenState = signal<string | null>(null);

  readonly token = this.tokenState.asReadonly();

  authenticateForDevelopment() {
    return this.http.post<DevelopmentTokenResponse>(`${environment.apiUrl}/dev/token`, {}).pipe(
      tap(({ accessToken }) => {
        this.tokenState.set(accessToken);
      }),
    );
  }

  getToken(): string | null {
    return this.tokenState();
  }
}
