import { LocalizedDatePipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { TeamContext } from '@core/team/team-context';
import { AiAnalysis } from '../../components/ai-analysis/ai-analysis';
import { DashboardKpis } from '../../components/dashboard-kpis/dashboard-kpis';
import { DashboardRisks } from '../../components/dashboard-risks/dashboard-risks';
import { EngineeringMetrics } from '../../components/engineering-metrics/engineering-metrics';
import { HealthEvolution } from '../../components/health-evolution/health-evolution';
import { LatestReport } from '../../components/latest-report/latest-report';
import { DashboardStore } from '../../state/dashboard-store';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    LocalizedDatePipe,
    TranslatePipe,
    AiAnalysis,
    DashboardKpis,
    DashboardRisks,
    EngineeringMetrics,
    HealthEvolution,
    LatestReport,
    NzAlertModule,
    NzCardModule,
    NzEmptyModule,
    NzGridModule,
    NzSkeletonModule,
    NzTagModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly teamContext = inject(TeamContext);
  private readonly store = inject(DashboardStore);

  readonly dashboard = this.store.dashboard;
  readonly loading = this.store.loading;
  readonly error = this.store.error;

  constructor() {
    effect(() => {
      const teamId = this.teamContext.selectedTeamId();

      if (teamId) {
        this.store.load(teamId);
      }
    });
  }
}
