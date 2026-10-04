import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { I18nService } from '@core/i18n/i18n.service';
import { LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { TeamContext } from '@core/team/team-context';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { Team, UpdateTeamRequest } from '@domains/teams/models/team';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { EMPTY, merge, Subject, catchError, switchMap, tap } from 'rxjs';
import { TeamMembers } from '../../components/team-members/team-members';
import { TeamEngineeringData } from '../../components/team-engineering-data/team-engineering-data';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [
    TranslatePipe,
    LocalizedNumberPipe,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzDrawerModule,
    NzEmptyModule,
    NzFormModule,
    NzInputModule,
    NzPopconfirmModule,
    NzSpinModule,
    TeamMembers,
    TeamEngineeringData,
  ],
  templateUrl: './team.html',
  styleUrl: './team.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPage {
  private readonly i18n = inject(I18nService);
  private readonly teamApi = inject(TeamApi);
  private readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly reloadSelection = new Subject<number>();
  readonly loadedSelectionVersion = signal(-1);
  readonly loading = signal(false);
  readonly error = signal(false);
  readonly team = signal<Team | null>(null);
  readonly memberTotal = signal(0);
  readonly editTeamOpen = signal(false);
  readonly updatingTeam = signal(false);
  readonly deletingTeam = signal(false);
  readonly editTeamForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
  });

  constructor() {
    const routeTeamId = this.route.snapshot.paramMap.get('teamId');
    if (routeTeamId) this.teamContext.selectTeam(routeTeamId);
    merge(toObservable(this.teamContext.selectionVersion), this.reloadSelection)
      .pipe(
        tap(() => {
          this.loadedSelectionVersion.set(-1);
          this.editTeamOpen.set(false);
          this.updatingTeam.set(false);
          this.deletingTeam.set(false);
          this.error.set(false);
          this.team.set(null);
          this.memberTotal.set(0);
          this.loading.set(!!this.teamContext.selectedTeamId());
        }),
        switchMap((version) => {
          const teamId = this.teamContext.selectedTeamId();
          if (!teamId) return EMPTY;
          return this.teamApi.getTeam(teamId).pipe(
            tap((team) => {
              if (!this.isCurrentSelection(teamId, version)) return;
              this.loadedSelectionVersion.set(version);
              this.team.set(team);
              this.loading.set(false);
            }),
            catchError((error) => {
              if (!this.isCurrentSelection(teamId, version)) return EMPTY;
              console.error('Failed to load team', error);
              this.message.error(this.i18n.t('team.notifications.loadFailed'));
              this.error.set(true);
              this.loading.set(false);
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  retryTeam(): void {
    this.reloadSelection.next(this.teamContext.selectionVersion());
  }

  openEditTeam(): void {
    const currentTeam = this.team();
    if (!currentTeam) return;
    this.editTeamForm.reset({ name: currentTeam.name, description: currentTeam.description ?? '' });
    this.editTeamOpen.set(true);
  }

  closeEditTeam(): void {
    if (!this.updatingTeam()) this.editTeamOpen.set(false);
  }

  updateTeam(): void {
    const teamId = this.teamContext.selectedTeamId();
    const version = this.teamContext.selectionVersion();
    if (
      !teamId ||
      this.team()?.id !== teamId ||
      this.loadedSelectionVersion() !== version ||
      this.updatingTeam() ||
      this.editTeamForm.invalid
    ) {
      this.editTeamForm.markAllAsTouched();
      return;
    }
    const value = this.editTeamForm.getRawValue();
    const request: UpdateTeamRequest = {
      name: value.name.trim(),
      description: value.description.trim() || null,
    };
    this.updatingTeam.set(true);
    this.teamApi
      .updateTeam(teamId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updatedTeam) => {
          if (!this.isCurrentSelection(teamId, version)) return;
          this.team.set(updatedTeam);
          this.updatingTeam.set(false);
          this.editTeamOpen.set(false);
          this.message.success(this.i18n.t('team.notifications.teamUpdated'));
        },
        error: (error) => {
          if (!this.isCurrentSelection(teamId, version)) return;
          console.error('Failed to update team', error);
          this.updatingTeam.set(false);
          this.message.error(this.i18n.t('team.notifications.teamUpdateFailed'));
        },
      });
  }

  deleteTeam(): void {
    const currentTeam = this.team();
    const version = this.teamContext.selectionVersion();
    if (
      !currentTeam ||
      !this.isCurrentSelection(currentTeam.id, version) ||
      this.loadedSelectionVersion() !== version ||
      this.deletingTeam()
    )
      return;
    this.deletingTeam.set(true);
    this.teamApi
      .deleteTeam(currentTeam.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          if (!this.isCurrentSelection(currentTeam.id, version)) return;
          this.deletingTeam.set(false);
          this.teamContext.clearTeam();
          this.message.success(this.i18n.t('team.notifications.teamDeleted'));
          void this.router.navigate(['/team']);
        },
        error: (error) => {
          if (!this.isCurrentSelection(currentTeam.id, version)) return;
          console.error('Failed to delete team', error);
          this.deletingTeam.set(false);
          this.message.error(this.i18n.t('team.notifications.teamDeleteFailed'));
        },
      });
  }

  private isCurrentSelection(teamId: string, version: number): boolean {
    return (
      this.teamContext.selectedTeamId() === teamId &&
      this.teamContext.selectionVersion() === version
    );
  }
}
