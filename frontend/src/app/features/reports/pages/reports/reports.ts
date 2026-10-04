import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';

import { TeamContext } from '@core/team/team-context';
import { ReportHistoryItem } from '../../models/report-history-item';
import { PaginationState } from '@shared/pagination/pagination-state';
import { TablePagination } from '@shared/ui/table-pagination/table-pagination';
import { ReportsApi } from '../../services/reports-api';

import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';

import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    TablePagination,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    TranslatePipe,
    RouterLink,
    NzEmptyModule,
    NzSkeletonModule,
  ],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Reports {
  readonly i18n = inject(I18nService);
  private readonly teamContext = inject(TeamContext);
  private readonly reportsApi = inject(ReportsApi);

  private readonly loadReports$ = new Subject<{ teamId: string; version: number } | null>();
  readonly pagination = new PaginationState();

  readonly reports = signal<readonly ReportHistoryItem[]>([]);
  readonly reportHistory = this.reports;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    this.loadReports$
      .pipe(
        switchMap((request) => {
          if (!request) return EMPTY;
          this.loading.set(true);
          this.error.set(null);
          return this.reportsApi
            .getReportsPage(
              request.teamId,
              this.pagination.pageNumber(),
              this.pagination.pageSize(),
            )
            .pipe(
              switchMap((page) => {
                if (request.version !== this.teamContext.selectionVersion()) return EMPTY;
                if (this.pagination.acceptTotal(page.totalCount)) {
                  this.retry();
                  return EMPTY;
                }
                this.reports.set(page.items);
                this.loading.set(false);
                return EMPTY;
              }),
              catchError((error) => {
                if (request.version === this.teamContext.selectionVersion()) {
                  this.reports.set([]);
                  if (error.status === 404) this.pagination.acceptTotal(0);
                  this.error.set(error.status === 404 ? null : 'reports.loadError');
                  this.loading.set(false);
                }
                return EMPTY;
              }),
            );
        }),
        takeUntilDestroyed(),
      )
      .subscribe();

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();
      const version = this.teamContext.selectionVersion();
      untracked(() => {
        this.pagination.reset();
        this.reports.set([]);
        this.error.set(null);
        this.loading.set(false);
        this.loadReports$.next(teamId ? { teamId, version } : null);
      });
    });
  }

  retry(): void {
    const teamId = this.teamContext.selectedTeamId();
    this.loadReports$.next(
      teamId ? { teamId, version: this.teamContext.selectionVersion() } : null,
    );
  }

  changePage(page: number): void {
    if (this.pagination.changePage(page)) this.retry();
  }

  changePageSize(size: number): void {
    if (this.pagination.changePageSize(size)) this.retry();
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

    const value = this.i18n.formatNumber(delta, '1.0-20');
    return delta > 0 ? `+${value}` : value;
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
}
