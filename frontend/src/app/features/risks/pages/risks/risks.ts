import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';
import { PaginationState } from '@core/models/pagination-state';
import { TablePagination } from '@core/components/table-pagination/table-pagination';
import { PagedRisksResponse } from '../../models/paged-risks-response';

import { TeamContext } from '@core/team/team-context';
import type { EngineeringRisk } from '@domains/engineering/models/risk';
import { RisksApi } from '../../services/risks-api';

import { UpperCasePipe } from '@angular/common';
import { I18nService } from '@core/i18n/i18n.service';
import { engineeringCategoryLabel } from '@core/i18n/engineering-category-label';
import { LocalizedDatePipe, LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSpinModule } from 'ng-zorro-antd/spin';

@Component({
  selector: 'app-risks',
  standalone: true,
  imports: [
    TablePagination,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    TranslatePipe,
    UpperCasePipe,
    RouterLink,
    NzAlertModule,
    NzEmptyModule,
    NzSpinModule,
  ],
  templateUrl: './risks.html',
  styleUrl: './risks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Risks {
  readonly i18n = inject(I18nService);
  private readonly risksApi = inject(RisksApi);
  private readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal(false);
  readonly data = signal<PagedRisksResponse | null>(null);
  readonly risks = signal<readonly EngineeringRisk[]>([]);

  private readonly loadRisks$ = new Subject<{ teamId: string; version: number } | null>();
  readonly pagination = new PaginationState();
  readonly sortedRisks = this.risks;

  constructor() {
    this.loadRisks$
      .pipe(
        switchMap((request) => {
          if (!request) return EMPTY;
          this.loading.set(true);
          this.error.set(false);
          this.data.set(null);
          this.risks.set([]);

          return this.risksApi
            .getRisksPage(request.teamId, this.pagination.pageNumber(), this.pagination.pageSize())
            .pipe(
              switchMap((response) => {
                if (request.version !== this.teamContext.selectionVersion()) return EMPTY;
                if (this.pagination.acceptTotal(response.page.totalCount)) {
                  this.retry();
                  return EMPTY;
                }
                this.data.set(response);
                this.risks.set(response.page.items);
                this.loading.set(false);
                return EMPTY;
              }),
              catchError((error) => {
                if (request.version === this.teamContext.selectionVersion()) {
                  this.data.set(null);
                  this.risks.set([]);
                  if (error.status === 404) this.pagination.acceptTotal(0);
                  this.error.set(error.status !== 404);
                  this.loading.set(false);
                }
                return EMPTY;
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();
      const version = this.teamContext.selectionVersion();
      untracked(() => {
        this.pagination.reset();
        this.loading.set(false);
        this.error.set(false);
        this.data.set(null);
        this.risks.set([]);
        this.loadRisks$.next(teamId ? { teamId, version } : null);
      });
    });
  }

  readonly criticalCount = computed(() => this.data()?.summary.criticalCount ?? 0);

  readonly highCount = computed(() => this.data()?.summary.highCount ?? 0);

  readonly mediumCount = computed(() => this.data()?.summary.mediumCount ?? 0);

  readonly lowCount = computed(() => this.data()?.summary.lowCount ?? 0);

  readonly totalCount = this.pagination.totalCount;

  retry(): void {
    const teamId = this.teamContext.selectedTeamId();
    this.loadRisks$.next(teamId ? { teamId, version: this.teamContext.selectionVersion() } : null);
  }

  changePage(page: number): void {
    if (this.pagination.changePage(page)) this.retry();
  }

  changePageSize(size: number): void {
    if (this.pagination.changePageSize(size)) this.retry();
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

  categoryLabel(category: string): string {
    return engineeringCategoryLabel(this.i18n, category);
  }
}
