import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { Router } from '@angular/router';
import { TeamContext } from '@core/team/team-context';
import { CreateTeamRequest, Team } from '../../models/team.model';
import { TeamApi } from '../../services/team-api';

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzDrawerModule,
    NzEmptyModule,
    NzFormModule,
    NzIconModule,
    NzInputModule,
    NzSpinModule,
  ],
  templateUrl: './teams.html',
  styleUrl: './teams.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamsPage {
  private readonly teamApi = inject(TeamApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly teamContext = inject(TeamContext);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly teams = signal<readonly Team[]>([]);

  readonly createTeamOpen = signal(false);
  readonly creatingTeam = signal(false);

  readonly createTeamForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),

    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
  });

  readonly deletingTeam = signal(false);

  constructor() {
    this.loadTeams();
  }

  private loadTeams(): void {
    this.loading.set(true);
    this.error.set(false);

    this.teamApi
      .getTeams()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (teams) => {
          this.teams.set(teams);
          this.loading.set(false);
        },
        error: (error) => {
          console.error('Failed to load teams', error);
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  openTeam(team: Team): void {
    this.teamContext.selectTeam(team.id);
    void this.router.navigate(['/team', team.id]);
  }

  openCreateTeam(): void {
    this.createTeamForm.reset({
      name: '',
      description: '',
    });

    this.createTeamOpen.set(true);
  }

  closeCreateTeam(): void {
    if (this.creatingTeam()) {
      return;
    }

    this.createTeamOpen.set(false);
  }

  createTeam(): void {
    if (this.createTeamForm.invalid) {
      this.createTeamForm.markAllAsTouched();
      return;
    }

    const value = this.createTeamForm.getRawValue();
    const description = value.description.trim();

    const request: CreateTeamRequest = {
      name: value.name.trim(),
      description: description || null,
    };

    this.creatingTeam.set(true);

    this.teamApi
      .createTeam(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (team) => {
          this.teams.update((teams) => [...teams, team]);

          this.creatingTeam.set(false);
          this.createTeamOpen.set(false);

          this.teamContext.selectTeam(team.id);

          void this.router.navigate(['/team', team.id]);
        },

        error: (error) => {
          console.error('Failed to create team', error);

          this.creatingTeam.set(false);
        },
      });
  }
}
