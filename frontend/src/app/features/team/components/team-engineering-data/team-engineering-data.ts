import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TeamContext } from '@core/team/team-context';
import {
  EngineeringMetric,
  MetricDataStatus,
  MetricType,
} from '@domains/engineering/models/metric';
import { ReportsApi } from '@features/reports/services/reports-api';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject, takeUntil } from 'rxjs';
import { MetricsApi } from '../../services/metrics-api';

@Component({
  selector: 'app-team-engineering-data',
  imports: [
    TranslatePipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
  ],
  templateUrl: './team-engineering-data.html',
  styleUrl: './team-engineering-data.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamEngineeringData {
  readonly teamId = input.required<string>();
  readonly selectionVersion = input.required<number>();
  private readonly metricsApi = inject(MetricsApi);
  private readonly reportsApi = inject(ReportsApi);
  private readonly context = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
  private readonly router = inject(Router);
  private readonly changed = new Subject<void>();
  readonly metrics = signal<readonly EngineeringMetric[]>([]);
  readonly calculatingMetrics = signal(false);
  readonly metricsError = signal(false);
  readonly calculatedMetricsPeriod = signal<{ periodStart: string; periodEnd: string } | null>(
    null,
  );
  readonly generatingReport = signal(false);
  readonly reportGenerationError = signal(false);
  readonly metricTypes: readonly MetricType[] = [
    'CycleTime',
    'PRReviewTime',
    'DeploymentFrequency',
    'ChangeFailureRate',
    'LeadTime',
    'OpenPRs',
    'MergedPRs',
    'BlockedItems',
  ];
  readonly metricsPeriodForm = new FormGroup({
    periodStart: new FormControl(this.defaultPeriodStart(), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    periodEnd: new FormControl(this.today(), {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  constructor() {
    effect(() => {
      this.teamId();
      this.selectionVersion();
      this.context.selectionVersion();
      this.changed.next();
      this.metrics.set([]);
      this.calculatedMetricsPeriod.set(null);
      this.calculatingMetrics.set(false);
      this.metricsError.set(false);
      this.generatingReport.set(false);
      this.reportGenerationError.set(false);
      this.metricsPeriodForm.reset({
        periodStart: this.defaultPeriodStart(),
        periodEnd: this.today(),
      });
    });
  }

  hasValidCalculatedMetrics(): boolean {
    const period = this.calculatedMetricsPeriod();
    if (!period || this.metrics().length === 0) return false;
    const { periodStart, periodEnd } = this.metricsPeriodForm.getRawValue();
    return period.periodStart === periodStart && period.periodEnd === periodEnd;
  }

  calculateMetrics(): void {
    const teamId = this.teamId();
    const version = this.selectionVersion();
    if (
      !this.current(teamId, version) ||
      this.metricsPeriodForm.invalid ||
      this.calculatingMetrics() ||
      this.generatingReport()
    ) {
      this.metricsPeriodForm.markAllAsTouched();
      return;
    }
    const { periodStart, periodEnd } = this.metricsPeriodForm.getRawValue();
    if (periodStart > periodEnd) {
      this.message.warning(this.i18n.t('team.notifications.invalidPeriod'));
      return;
    }
    this.calculatingMetrics.set(true);
    this.metricsError.set(false);
    this.reportGenerationError.set(false);
    this.metricsApi
      .calculate(teamId, periodStart, periodEnd)
      .pipe(takeUntil(this.changed), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (metrics) => {
          if (!this.current(teamId, version)) return;
          this.metrics.set(metrics);
          this.calculatedMetricsPeriod.set({ periodStart, periodEnd });
          this.calculatingMetrics.set(false);
          const available = metrics.filter((metric) => metric.dataStatus === 'Available').length;
          this.message.success(
            this.i18n.t('team.notifications.metricsCalculated', {
              available: this.i18n.formatNumber(available),
              total: this.i18n.formatNumber(metrics.length),
            }),
          );
        },
        error: (error) => {
          if (!this.current(teamId, version)) return;
          console.error('Failed to calculate engineering metrics', error);
          this.calculatingMetrics.set(false);
          this.metricsError.set(true);
          this.message.error(this.i18n.t('team.notifications.metricsFailed'));
        },
      });
  }

  generateReport(): void {
    const teamId = this.teamId();
    const version = this.selectionVersion();
    const period = this.calculatedMetricsPeriod();
    if (
      !this.current(teamId, version) ||
      !period ||
      !this.hasValidCalculatedMetrics() ||
      this.generatingReport()
    )
      return;
    this.generatingReport.set(true);
    this.reportGenerationError.set(false);
    this.reportsApi
      .generateReport(teamId, period.periodStart, period.periodEnd)
      .pipe(takeUntil(this.changed), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          if (!this.current(teamId, version)) return;
          this.generatingReport.set(false);
          this.message.success(this.i18n.t('team.notifications.reportGenerated'));
          void this.router.navigate(['/reports', report.id]);
        },
        error: (error) => {
          if (!this.current(teamId, version)) return;
          console.error('Failed to generate engineering report', error);
          this.generatingReport.set(false);
          this.reportGenerationError.set(true);
          this.message.error(this.i18n.t('team.notifications.reportFailed'));
        },
      });
  }

  metric(metricType: MetricType): EngineeringMetric | null {
    return this.metrics().find((metric) => metric.metricType === metricType) ?? null;
  }

  metricLabel(metricType: MetricType): string {
    return this.i18n.t(`metrics.${metricType}`);
  }
  metricStatusLabel(status: MetricDataStatus): string {
    return this.i18n.t(`team.metricStatuses.${status}`);
  }

  metricValue(metric: EngineeringMetric): string {
    if (metric.dataStatus !== 'Available' || metric.value === null) return '—';
    const value = this.i18n.formatNumber(metric.value, '1.0-20');
    switch (metric.metricType) {
      case 'CycleTime':
      case 'PRReviewTime':
      case 'LeadTime':
        return this.i18n.t('team.metricHours', { value });
      case 'ChangeFailureRate':
        return this.i18n.t('team.metricPercent', { value });
      case 'DeploymentFrequency':
      case 'OpenPRs':
      case 'MergedPRs':
      case 'BlockedItems':
        return value;
    }
  }

  availableMetricCount(): number {
    return this.metrics().filter((metric) => metric.dataStatus === 'Available').length;
  }

  private current(teamId: string, version: number): boolean {
    return (
      this.context.selectedTeamId() === teamId &&
      this.context.selectionVersion() === version &&
      this.teamId() === teamId &&
      this.selectionVersion() === version
    );
  }

  private today(): string {
    return this.toDateInputValue(new Date());
  }
  private defaultPeriodStart(): string {
    const date = new Date();
    date.setDate(date.getDate() - 29);
    return this.toDateInputValue(date);
  }
  private toDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
