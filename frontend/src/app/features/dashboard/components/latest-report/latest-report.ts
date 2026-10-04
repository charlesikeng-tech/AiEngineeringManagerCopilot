import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedNumberPipe, LocalizedPercentPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { EngineeringReport } from '@domains/engineering/models/report';

@Component({
  selector: 'app-latest-report',
  standalone: true,
  imports: [TranslatePipe, LocalizedNumberPipe, LocalizedPercentPipe, RouterLink, NzCardModule, NzEmptyModule, NzProgressModule, NzTagModule],
  templateUrl: './latest-report.html',
  styleUrl: './latest-report.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LatestReport {
  private readonly i18n = inject(I18nService);
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
    return new Intl.DateTimeFormat(this.i18n.locale(), {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(new Date(`${value}T00:00:00`));
  }
}
