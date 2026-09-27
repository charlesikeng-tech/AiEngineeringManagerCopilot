import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';

import { EngineeringRisk, RiskSeverity } from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-dashboard-risks',
  standalone: true,
  imports: [NzCardModule, NzTagModule],
  templateUrl: './dashboard-risks.html',
  styleUrl: './dashboard-risks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardRisks {
  readonly risks = input.required<readonly EngineeringRisk[]>();

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
