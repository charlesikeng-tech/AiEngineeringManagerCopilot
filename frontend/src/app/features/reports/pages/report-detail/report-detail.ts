import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, switchMap } from 'rxjs';

import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

import { HttpErrorResponse } from '@angular/common/http';
import { TeamContext } from '../../../../core/team/team-context';
import {
  AIAnalysis,
  EngineeringReport,
} from '../../../dashboard/models/engineering-dashboard-response';
import { ReportAnalysisApi } from '../../services/report-analysis-api';
import { ReportsApi } from '../../services/reports-api';

@Component({
  selector: 'app-report-detail',
  standalone: true,
  imports: [PercentPipe, DatePipe, DecimalPipe, RouterLink, NzEmptyModule, NzSkeletonModule],
  templateUrl: './report-detail.html',
  styleUrl: './report-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly teamContext = inject(TeamContext);
  private readonly reportsApi = inject(ReportsApi);
  private readonly reportAnalysisApi = inject(ReportAnalysisApi);
  private readonly destroyRef = inject(DestroyRef);

  private readonly loadReport$ = new Subject<{
    teamId: string;
    reportId: string;
  }>();

  readonly report = signal<EngineeringReport | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly analysis = signal<AIAnalysis | null>(null);
  readonly analysisLoading = signal(false);
  readonly analysisUnavailable = signal(false);
  readonly analysisError = signal(false);

  constructor() {
    this.loadReport$
      .pipe(
        switchMap(({ teamId, reportId }) => {
          this.loading.set(true);
          this.error.set(null);
          this.report.set(null);

          return this.reportsApi.getReport(teamId, reportId);
        }),
        takeUntilDestroyed(),
      )
      .subscribe({
        next: (report) => {
          this.report.set(report);
          this.loading.set(false);

          this.loadAnalysis(report.teamId, report.id);
        },
        error: () => {
          this.report.set(null);
          this.error.set('Unable to load this engineering report.');
          this.loading.set(false);
        },
      });

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();
      const reportId = this.route.snapshot.paramMap.get('reportId');

      if (!teamId || !reportId) {
        return;
      }

      this.loadReport$.next({ teamId, reportId });
    });
  }

  private loadAnalysis(teamId: string, reportId: string): void {
    this.analysisLoading.set(true);
    this.analysisUnavailable.set(false);
    this.analysisError.set(false);
    this.analysis.set(null);

    this.reportAnalysisApi
      .getAnalysis(teamId, reportId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (analysis) => {
          this.analysis.set(analysis);
          this.analysisLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.analysisLoading.set(false);

          if (error.status === 404) {
            this.analysisUnavailable.set(true);
            return;
          }

          this.analysisError.set(true);
        },
      });
  }
}
