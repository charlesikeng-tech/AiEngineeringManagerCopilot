import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSelectModule } from 'ng-zorro-antd/select';

import { Team } from '../models/team';
import { TeamApi } from '../team-api';
import { TeamContext } from '../team-context';

@Component({
  selector: 'app-team-selector',
  standalone: true,
  imports: [FormsModule, NzIconModule, NzSelectModule, TranslatePipe],
  templateUrl: './team-selector.html',
  styleUrl: './team-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamSelector {
  private readonly teamApi = inject(TeamApi);
  private readonly router = inject(Router);

  readonly teamContext = inject(TeamContext);

  readonly teams = signal<readonly Team[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.teamApi
      .getTeams()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (teams) => {
          this.teams.set(teams);

          const selectedTeamId = this.teamContext.selectedTeamId();

          const selectedTeamStillExists =
            selectedTeamId !== null && teams.some((team) => team.id === selectedTeamId);

          if (!selectedTeamStillExists && teams.length > 0) {
            this.teamContext.selectTeam(teams[0].id);
          }

          if (teams.length === 0) {
            this.teamContext.clearTeam();
          }

          this.loading.set(false);
        },

        error: () => {
          this.loading.set(false);
        },
      });
  }

  selectTeam(teamId: string | null): void {
    if (!teamId) {
      return;
    }

    this.teamContext.selectTeam(teamId);

    if (this.router.url.startsWith('/team/')) {
      void this.router.navigate(['/team', teamId]);
    }
  }
}
