import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzCollapseModule } from 'ng-zorro-antd/collapse';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzTypographyModule } from 'ng-zorro-antd/typography';

import { AIAnalysis, LlmEvidence } from '../../models/engineering-dashboard-response';

@Component({
  selector: 'app-ai-analysis',
  standalone: true,
  imports: [
    NzAlertModule,
    NzCardModule,
    NzCollapseModule,
    NzEmptyModule,
    NzListModule,
    NzProgressModule,
    NzTagModule,
    NzTypographyModule,
  ],
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
