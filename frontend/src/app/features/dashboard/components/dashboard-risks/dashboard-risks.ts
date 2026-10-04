import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';

import { EngineeringRisk, RiskSeverity } from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-dashboard-risks',
  standalone: true,
  imports: [TranslatePipe, LocalizedNumberPipe, NzCardModule, NzTagModule, RouterLink],
  templateUrl: './dashboard-risks.html',
  styleUrl: './dashboard-risks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardRisks {
  readonly risks = input.required<readonly EngineeringRisk[]>();
  readonly visibleRisks = computed(() => {
    const severityOrder: Record<RiskSeverity, number> = { Critical: 0, High: 1, Medium: 2, Low: 3 };
    return [...this.risks()]
      .sort((left, right) => severityOrder[left.severity] - severityOrder[right.severity])
      .slice(0, 5);
  });

  severityColor(severity: RiskSeverity): 'default' | 'blue' | 'orange' | 'red' | 'magenta' {
    switch (severity) {
      case 'Low':
        return 'blue';

      case 'Medium':
        return 'orange';

      case 'High':
        return 'red';

      case 'Critical':
        return 'magenta';

      default:
        return 'default';
    }
  }

  severityClass(severity: RiskSeverity): string {
    switch (severity) {
      case 'Critical':
        return 'risk-critical';

      case 'High':
        return 'risk-high';

      case 'Medium':
        return 'risk-medium';

      case 'Low':
        return 'risk-low';
    }
  }
}
