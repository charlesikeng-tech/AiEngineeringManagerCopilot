import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap } from 'rxjs';

import { TeamContext } from '../../../../core/team/team-context';
import { EngineeringReport } from '../../../dashboard/models/engineering-dashboard-response';
import { ReportsApi } from '../../services/reports-api';

import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';


type ReportHistoryItem = EngineeringReport & {
  scoreDelta: number | null;
  coverageDelta: number | null;
};

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink, NzEmptyModule, NzSkeletonModule],
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

  readonly reportHistory = computed<readonly ReportHistoryItem[]>(() => {
    const reports = this.reports();

    return reports.map((report, index) => {
      // Reports are sorted newest -> oldest. The next item is therefore
      // the previous reporting period.
      const previousReport = reports[index + 1];

      return {
        ...report,
        scoreDelta: previousReport
          ? report.overallScore - previousReport.overallScore
          : null,
        coverageDelta: previousReport
          ? report.dataCoverage - previousReport.dataCoverage
          : null,
      };
    });
  });

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

  deltaDirection(delta: number | null): 'up' | 'down' | 'flat' | 'none' {
    if (delta === null) {
      return 'none';
    }

    if (delta > 0) {
      return 'up';
    }

    if (delta < 0) {
      return 'down';
    }

    return 'flat';
  }

  deltaValue(delta: number | null): string {
    if (delta === null) {
      return '—';
    }

    if (delta > 0) {
      return `+${delta}`;
    }

    return `${delta}`;
  }

}
