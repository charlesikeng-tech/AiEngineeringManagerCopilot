import { I18nService } from '@core/i18n/i18n.service';
import { engineeringCategoryLabel } from '@core/i18n/engineering-category-label';
import {
  LocalizedDatePipe,
  LocalizedNumberPipe,
  LocalizedPercentPipe,
} from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
  computed,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

import { TeamContext } from '@core/team/team-context';
import {
  AIAnalysis,
  EngineeringReport,
} from '@features/dashboard/models/engineering-dashboard-response';
import { ReportAnalysisApi } from '../../services/report-analysis-api';
import { ReportsApi } from '../../services/reports-api';

@Component({
  selector: 'app-report-detail',
  standalone: true,
  imports: [
    LocalizedPercentPipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    TranslatePipe,
    RouterLink,
    NzEmptyModule,
    NzSkeletonModule,
  ],
  templateUrl: './report-detail.html',
  styleUrl: './report-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportDetail {
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);
  private readonly teamContext = inject(TeamContext);
  private readonly reportsApi = inject(ReportsApi);
  private readonly reportAnalysisApi = inject(ReportAnalysisApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly routeParams = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap,
  });
  private readonly routeQuery = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });
  private readonly reportId = computed(
    () => this.routeParams().get('reportId')?.toLowerCase() ?? null,
  );
  private readonly linkedTeamId = computed(
    () => this.routeQuery().get('teamId')?.toLowerCase() ?? null,
  );
  private readonly reportRequestsChanged = new Subject<void>();

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
      this.reportId();
      const linkedTeamId = this.linkedTeamId();
      untracked(() => {
        if (linkedTeamId && this.validLinkedTeam(linkedTeamId)) {
          this.teamContext.selectTeam(linkedTeamId);
        }
      });
    });

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();
      const reportId = this.reportId();
      const linkedTeamId = this.linkedTeamId();
      const version = this.teamContext.selectionVersion();
      untracked(() => {
        this.reportRequestsChanged.next();
        this.resetReportState();
        if (linkedTeamId && !this.validLinkedTeam(linkedTeamId)) {
          this.error.set('reports.detailLoadError');
          return;
        }
        if (linkedTeamId && linkedTeamId !== teamId) {
          this.error.set('reports.detailLoadError');
          return;
        }
        if (teamId && reportId) this.loadReport(teamId, reportId, version);
      });
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
    const version = this.teamContext.selectionVersion();
    if (!this.isCurrentReport(teamId, reportId, version)) return;

    this.analysisGenerating.set(true);
    this.analysisError.set(false);
    this.analysisUnavailable.set(false);

    this.reportAnalysisApi
      .generateAnalysis(teamId, reportId)
      .pipe(takeUntil(this.reportRequestsChanged), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (analysis) => {
          if (!this.isCurrentReport(teamId, reportId, version)) {
            return;
          }

          this.analysis.set(analysis);

          this.analysisGenerating.set(false);
          this.analysisUnavailable.set(false);
          this.analysisError.set(false);
        },

        error: (error: HttpErrorResponse) => {
          if (!this.isCurrentReport(teamId, reportId, version)) {
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

  private loadReport(teamId: string, reportId: string, version: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.report.set(null);

    this.resetAnalysisState();

    this.reportsApi
      .getReport(teamId, reportId)
      .pipe(takeUntil(this.reportRequestsChanged), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          if (
            this.teamContext.selectedTeamId() !== teamId ||
            this.reportId() !== reportId ||
            (this.linkedTeamId() !== null && this.linkedTeamId() !== teamId) ||
            this.teamContext.selectionVersion() !== version
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
            this.reportId() !== reportId ||
            (this.linkedTeamId() !== null && this.linkedTeamId() !== teamId) ||
            this.teamContext.selectionVersion() !== version
          ) {
            return;
          }

          console.error('Failed to load engineering report', error);

          this.report.set(null);

          this.error.set('reports.detailLoadError');

          this.loading.set(false);

          this.resetAnalysisState();
        },
      });
  }

  // ---------------------------------------------------------------------------
  // AI loading
  // ---------------------------------------------------------------------------

  private loadAnalysis(teamId: string, reportId: string): void {
    const version = this.teamContext.selectionVersion();
    if (!this.isCurrentReport(teamId, reportId, version)) return;
    this.analysisLoading.set(true);

    this.analysisUnavailable.set(false);
    this.analysisError.set(false);

    this.analysis.set(null);

    this.reportAnalysisApi
      .getAnalysis(teamId, reportId)
      .pipe(takeUntil(this.reportRequestsChanged), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (analysis) => {
          if (!this.isCurrentReport(teamId, reportId, version)) {
            return;
          }

          this.analysis.set(analysis);

          this.analysisLoading.set(false);
          this.analysisUnavailable.set(false);
          this.analysisError.set(false);
        },

        error: (error: HttpErrorResponse) => {
          if (!this.isCurrentReport(teamId, reportId, version)) {
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

  private isCurrentReport(teamId: string, reportId: string, version: number): boolean {
    const currentReport = this.report();

    return (
      currentReport?.teamId === teamId &&
      currentReport.id === reportId &&
      this.teamContext.selectedTeamId() === teamId &&
      this.reportId() === reportId &&
      (this.linkedTeamId() === null || this.linkedTeamId() === teamId) &&
      this.teamContext.selectionVersion() === version
    );
  }

  private validLinkedTeam(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
  }

  metricLabel(metricType: string): string {
    const knownMetrics = [
      'CycleTime',
      'PRReviewTime',
      'DeploymentFrequency',
      'ChangeFailureRate',
      'LeadTime',
      'OpenPRs',
      'MergedPRs',
      'BlockedItems',
    ];
    return knownMetrics.includes(metricType) ? this.i18n.t(`metrics.${metricType}`) : metricType;
  }

  healthLabel(healthLevel: string): string {
    const knownLevels = [
      'Healthy',
      'Needs Attention',
      'At Risk',
      'No Data',
      'Attention',
      'Critical',
      'Excellent',
      'Good',
      'Warning',
      'Unknown',
    ];
    return knownLevels.includes(healthLevel)
      ? this.i18n.t(`healthLevels.${healthLevel}`)
      : healthLevel;
  }

  categoryLabel(category: string): string {
    return engineeringCategoryLabel(this.i18n, category);
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
