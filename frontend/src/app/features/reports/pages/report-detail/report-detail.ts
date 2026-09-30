import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
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

import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

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

  // ---------------------------------------------------------------------------
  // Report
  // ---------------------------------------------------------------------------

  readonly report = signal<EngineeringReport | null>(null);

  readonly loading = signal(false);

  readonly error = signal<string | null>(null);

  // ---------------------------------------------------------------------------
  // AI analysis
  // ---------------------------------------------------------------------------

  readonly analysis = signal<AIAnalysis | null>(null);

  readonly analysisLoading = signal(false);

  readonly analysisUnavailable = signal(false);

  readonly analysisError = signal(false);

  readonly analysisGenerating = signal(false);

  constructor() {
    effect(() => {
      const teamId = this.teamContext.selectedTeamId();

      const reportId = this.route.snapshot.paramMap.get('reportId');

      if (!teamId || !reportId) {
        this.resetReportState();
        return;
      }

      this.loadReport(teamId, reportId);
    });
  }

  // ---------------------------------------------------------------------------
  // Public AI actions
  // ---------------------------------------------------------------------------

  generateAnalysis(): void {
    const currentReport = this.report();

    if (!currentReport || this.analysisGenerating() || this.analysisLoading()) {
      return;
    }

    const teamId = currentReport.teamId;
    const reportId = currentReport.id;

    this.analysisGenerating.set(true);
    this.analysisError.set(false);
    this.analysisUnavailable.set(false);

    this.reportAnalysisApi
      .generateAnalysis(teamId, reportId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (analysis) => {
          if (!this.isCurrentReport(teamId, reportId)) {
            return;
          }

          this.analysis.set(analysis);

          this.analysisGenerating.set(false);
          this.analysisUnavailable.set(false);
          this.analysisError.set(false);
        },

        error: (error: HttpErrorResponse) => {
          if (!this.isCurrentReport(teamId, reportId)) {
            return;
          }

          console.error('Failed to generate AI analysis', error);

          this.analysisGenerating.set(false);
          this.analysisError.set(true);
        },
      });
  }

  refreshAnalysis(): void {
    const currentReport = this.report();

    if (!currentReport || this.analysisLoading() || this.analysisGenerating()) {
      return;
    }

    this.loadAnalysis(currentReport.teamId, currentReport.id);
  }

  // ---------------------------------------------------------------------------
  // Report loading
  // ---------------------------------------------------------------------------

  private loadReport(teamId: string, reportId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.report.set(null);

    this.resetAnalysisState();

    this.reportsApi
      .getReport(teamId, reportId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          if (
            this.teamContext.selectedTeamId() !== teamId ||
            this.route.snapshot.paramMap.get('reportId') !== reportId
          ) {
            return;
          }

          this.report.set(report);
          this.loading.set(false);

          this.loadAnalysis(report.teamId, report.id);
        },

        error: (error) => {
          if (
            this.teamContext.selectedTeamId() !== teamId ||
            this.route.snapshot.paramMap.get('reportId') !== reportId
          ) {
            return;
          }

          console.error('Failed to load engineering report', error);

          this.report.set(null);

          this.error.set('Unable to load this engineering report.');

          this.loading.set(false);

          this.resetAnalysisState();
        },
      });
  }

  // ---------------------------------------------------------------------------
  // AI loading
  // ---------------------------------------------------------------------------

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
          if (!this.isCurrentReport(teamId, reportId)) {
            return;
          }

          this.analysis.set(analysis);

          this.analysisLoading.set(false);
          this.analysisUnavailable.set(false);
          this.analysisError.set(false);
        },

        error: (error: HttpErrorResponse) => {
          if (!this.isCurrentReport(teamId, reportId)) {
            return;
          }

          this.analysisLoading.set(false);

          if (error.status === 404) {
            this.analysis.set(null);
            this.analysisUnavailable.set(true);
            return;
          }

          console.error('Failed to load AI analysis', error);

          this.analysisError.set(true);
        },
      });
  }

  // ---------------------------------------------------------------------------
  // Guards
  // ---------------------------------------------------------------------------

  private isCurrentReport(teamId: string, reportId: string): boolean {
    const currentReport = this.report();

    return (
      currentReport?.teamId === teamId &&
      currentReport.id === reportId &&
      this.teamContext.selectedTeamId() === teamId &&
      this.route.snapshot.paramMap.get('reportId') === reportId
    );
  }

  // ---------------------------------------------------------------------------
  // State
  // ---------------------------------------------------------------------------

  private resetAnalysisState(): void {
    this.analysis.set(null);

    this.analysisLoading.set(false);
    this.analysisUnavailable.set(false);
    this.analysisError.set(false);
    this.analysisGenerating.set(false);
  }

  private resetReportState(): void {
    this.report.set(null);

    this.loading.set(false);
    this.error.set(null);

    this.resetAnalysisState();
  }
}
