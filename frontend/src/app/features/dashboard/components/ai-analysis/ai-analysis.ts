import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LocalizedNumberPipe, LocalizedPercentPipe } from '@core/i18n/localized-format.pipes';
import { TranslatePipe } from '@ngx-translate/core';

import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTagModule } from 'ng-zorro-antd/tag';

import { AIAnalysis, LlmEvidence } from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-ai-analysis',
  standalone: true,
  imports: [TranslatePipe, LocalizedNumberPipe, LocalizedPercentPipe, NzCardModule, NzEmptyModule, NzProgressModule, NzTagModule],
  templateUrl: './ai-analysis.html',
  styleUrl: './ai-analysis.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiAnalysis {
  readonly analysis = input.required<AIAnalysis | null>();

  confidencePercentage(evidence: LlmEvidence): number {
    return Math.round(evidence.confidence * 100);
  }
}
