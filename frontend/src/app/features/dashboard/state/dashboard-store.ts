import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, finalize, Subject, switchMap, tap } from 'rxjs';

import { EngineeringDashboardResponse } from '../models/engineering-dashboard-response';
import { DashboardApi } from '../services/dashboard-api';

@Injectable({
  providedIn: 'root',
})
export class DashboardStore {
  private readonly dashboardApi = inject(DashboardApi);
  private readonly destroyRef = inject(DestroyRef);

  private readonly dashboardState = signal<EngineeringDashboardResponse | null>(null);

  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);

  private readonly loadRequest = new Subject<string>();

  readonly dashboard = this.dashboardState.asReadonly();
  readonly loading = this.loadingState.asReadonly();
  readonly error = this.errorState.asReadonly();

  constructor() {
    this.loadRequest
      .pipe(
        tap(() => {
          this.loadingState.set(true);
          this.errorState.set(null);
        }),

        switchMap((teamId) =>
          this.dashboardApi.getDashboard(teamId).pipe(
            tap((dashboard) => {
              this.dashboardState.set(dashboard);
            }),

            catchError((error) => {
              console.error('Dashboard loading failed', error);

              this.dashboardState.set(null);

              this.errorState.set('dashboard.loadError');

              return EMPTY;
            }),

            finalize(() => {
              this.loadingState.set(false);
            }),
          ),
        ),

        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  load(teamId: string): void {
    this.loadRequest.next(teamId);
  }
}
