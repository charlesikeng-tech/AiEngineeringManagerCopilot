import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TeamContext } from '@core/team/team-context';
import { TeamSelector } from '@core/team/team-selector/team-selector';
import { TranslatePipe } from '@ngx-translate/core';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { GitHubIntegration } from '../github-integration/github-integration';
import { JiraIntegration } from '../jira-integration/jira-integration';
import { SlackIntegration } from '../slack-integration/slack-integration';
import { MicrosoftTeamsIntegration } from '../microsoft-teams-integration/microsoft-teams-integration';

@Component({
  selector: 'app-integration-settings',
  standalone: true,
  imports: [
    TeamSelector,
    TranslatePipe,
    NzEmptyModule,
    GitHubIntegration,
    JiraIntegration,
    SlackIntegration,
    MicrosoftTeamsIntegration,
  ],
  templateUrl: './integration-settings.html',
  styleUrl: './integration-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IntegrationSettings {
  readonly teamContext = inject(TeamContext);
}
