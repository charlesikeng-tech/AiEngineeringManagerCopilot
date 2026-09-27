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

import { TeamContext } from '../../../../core/team/team-context';
import {
  EngineeringRisk,
  EngineeringRisksResponse,
} from '../../../dashboard/models/engineering-dashboard-response';
import { RisksApi } from '../../services/risks-api';

import { DatePipe, UpperCasePipe } from '@angular/common';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSpinModule } from 'ng-zorro-antd/spin';

@Component({
  selector: 'app-risks',
  standalone: true,
  imports: [DatePipe, UpperCasePipe, NzAlertModule, NzEmptyModule, NzSpinModule],
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

  constructor() {
    effect(() => {
      const teamId = this.teamContext.selectedTeamId();

      this.load(teamId);
    });
  }

  private load(teamId: string | null): void {
    if (!teamId) {
      this.loading.set(false);
      this.error.set(false);
      this.data.set(null);
      this.risks.set([]);
      return;
    }

    this.loading.set(true);
    this.error.set(false);
    this.data.set(null);
    this.risks.set([]);

    this.risksApi
      .getCurrentRisks(teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
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
