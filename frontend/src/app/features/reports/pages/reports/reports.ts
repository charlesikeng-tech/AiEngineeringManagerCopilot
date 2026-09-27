import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap } from 'rxjs';

import { TeamContext } from '../../../../core/team/team-context';
import { EngineeringReport } from '../../../dashboard/models/engineering-dashboard-response';
import { ReportsApi } from '../../services/reports-api';

import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink, NzCardModule, NzEmptyModule, NzSkeletonModule],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Reports {
  private readonly teamContext = inject(TeamContext);
  private readonly reportsApi = inject(ReportsApi);

  private readonly loadReports$ = new Subject<string>();

  readonly reports = signal<readonly EngineeringReport[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    this.loadReports$
      .pipe(
        switchMap((teamId) => {
          this.loading.set(true);
          this.error.set(null);

          return this.reportsApi.getReports(teamId);
        }),
        takeUntilDestroyed(),
      )
      .subscribe({
        next: (reports) => {
          this.reports.set(
            [...reports].sort(
              (a, b) => new Date(b.periodEnd).getTime() - new Date(a.periodEnd).getTime(),
            ),
          );

          this.loading.set(false);
        },
        error: () => {
          this.reports.set([]);
          this.error.set('Unable to load engineering reports.');
          this.loading.set(false);
        },
      });

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();

      if (!teamId) {
        this.reports.set([]);
        return;
      }

      this.loadReports$.next(teamId);
    });
  }
}
