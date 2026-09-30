import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTagModule } from 'ng-zorro-antd/tag';

import { EngineeringReport } from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-latest-report',
  standalone: true,
  imports: [RouterLink, NzCardModule, NzEmptyModule, NzProgressModule, NzTagModule],
  templateUrl: './latest-report.html',
  styleUrl: './latest-report.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LatestReport {
  readonly report = input.required<EngineeringReport | null>();

  readonly coveragePercentage = computed(() => {
    const report = this.report();

    if (!report) {
      return 0;
    }

    return Math.round(report.dataCoverage);
  });

  readonly period = computed(() => {
    const report = this.report();

    if (!report) {
      return '';
    }

    return `${this.formatDate(report.periodStart)} – ${this.formatDate(report.periodEnd)}`;
  });

  private formatDate(value: string): string {
    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(new Date(`${value}T00:00:00`));
  }
}
