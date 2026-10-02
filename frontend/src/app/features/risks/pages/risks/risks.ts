import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subject, switchMap } from 'rxjs';

import { TeamContext } from '@core/team/team-context';
import {
  EngineeringRisk,
  EngineeringRisksResponse,
} from '@features/dashboard/models/engineering-dashboard-response';
import { RisksApi } from '../../services/risks-api';

import { DatePipe, UpperCasePipe } from '@angular/common';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSpinModule } from 'ng-zorro-antd/spin';

@Component({
  selector: 'app-risks',
  standalone: true,
  imports: [DatePipe, UpperCasePipe, RouterLink, NzAlertModule, NzEmptyModule, NzSpinModule],
  templateUrl: './risks.html',
  styleUrl: './risks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Risks {
  private readonly risksApi = inject(RisksApi);
  private readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal(false);
  readonly data = signal<EngineeringRisksResponse | null>(null);
  readonly risks = signal<readonly EngineeringRisk[]>([]);

  private readonly loadRisks$ = new Subject<string>();

  readonly sortedRisks = computed(() => {
    const severityOrder: Record<string, number> = {
      Critical: 0,
      High: 1,
      Medium: 2,
      Low: 3,
    };

    return [...this.risks()].sort(
      (a, b) => (severityOrder[a.severity] ?? 99) - (severityOrder[b.severity] ?? 99),
    );
  });

  constructor() {
    this.loadRisks$
      .pipe(
        switchMap((teamId) => {
          this.loading.set(true);
          this.error.set(false);
          this.data.set(null);
          this.risks.set([]);

          return this.risksApi.getCurrentRisks(teamId);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.data.set(response);
          this.risks.set(response.risks);
          this.loading.set(false);
        },
        error: () => {
          this.data.set(null);
          this.risks.set([]);
          this.error.set(true);
          this.loading.set(false);
        },
      });

    effect(() => {
      const teamId = this.teamContext.selectedTeamId();

      if (!teamId) {
        this.loading.set(false);
        this.error.set(false);
        this.data.set(null);
        this.risks.set([]);
        return;
      }

      this.loadRisks$.next(teamId);
    });
  }

  readonly criticalCount = computed(
    () => this.risks().filter((risk) => risk.severity === 'Critical').length,
  );

  readonly highCount = computed(
    () => this.risks().filter((risk) => risk.severity === 'High').length,
  );

  readonly mediumCount = computed(
    () => this.risks().filter((risk) => risk.severity === 'Medium').length,
  );

  readonly lowCount = computed(() => this.risks().filter((risk) => risk.severity === 'Low').length);

  readonly totalCount = computed(() => this.risks().length);

  metricLabel(metricType: string): string {
    const labels: Record<string, string> = {
      CycleTime: 'Cycle Time',
      PRReviewTime: 'PR Review Time',
      DeploymentFrequency: 'Deployment Frequency',
      ChangeFailureRate: 'Change Failure Rate',
      LeadTime: 'Lead Time',
      OpenPRs: 'Open PRs',
      MergedPRs: 'Merged PRs',
      BlockedItems: 'Blocked Items',
    };

    return labels[metricType] ?? metricType;
  }
}
