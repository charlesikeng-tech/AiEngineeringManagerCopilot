import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, of, tap, throwError, timeout } from 'rxjs';

import { environment } from '@environments/environment';

export interface AdministratorProfile {
  id: string;
  email: string | null;
  name: string;
  role: 'PlatformAdministrator' | 'User';
  emailVerified?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly tokenState = signal<string | null>(null);
  private readonly userState = signal<AdministratorProfile | null>(null);

  readonly token = this.tokenState.asReadonly();
  readonly user = this.userState.asReadonly();

  loadCurrent() {
    return this.http.get<AdministratorProfile>(`${environment.apiUrl}/auth/current`).pipe(
      timeout(10000),
      tap((user) => this.userState.set(user)),
      catchError((error: HttpErrorResponse) => {
        this.userState.set(null);
        return error.status === 401 ? of(null) : throwError(() => error);
      }),
    );
  }

  setupStatus() {
    return this.http.get<{ setupAvailable: boolean }>(`${environment.apiUrl}/auth/setup-status`).pipe(timeout(10000));
  }

  login(email: string, password: string) {
    return this.http.post<AdministratorProfile>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap((user) => this.userState.set(user)));
  }

  setup(secret: string, email: string, name: string, password: string) {
    return this.http.post<AdministratorProfile>(`${environment.apiUrl}/auth/setup`, { secret, email, name, password })
      .pipe(tap((user) => this.userState.set(user)));
  }

  logout() {
    return this.http.post<void>(`${environment.apiUrl}/auth/logout`, {}).pipe(
      catchError((error: HttpErrorResponse) => error.status === 401 ? of(undefined) : throwError(() => error)),
      tap(() => this.clearSession()),
    );
  }

  clearSession(): void {
    this.userState.set(null);
    this.tokenState.set(null);
  }

  getToken(): string | null {
    return this.tokenState();
  }
}
