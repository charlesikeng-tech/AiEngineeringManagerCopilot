import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { NzSelectModule } from 'ng-zorro-antd/select';

import { Team } from '../models/team';
import { TeamApi } from '../team-api';
import { TeamContext } from '../team-context';

import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-team-selector',
  standalone: true,
  imports: [FormsModule, NzSelectModule],
  templateUrl: './team-selector.html',
  styleUrl: './team-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamSelector {
  private readonly teamApi = inject(TeamApi);

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
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
        },
      });
  }

  selectTeam(teamId: string): void {
    this.teamContext.selectTeam(teamId);
  }
}
