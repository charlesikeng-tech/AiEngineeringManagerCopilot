import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';

import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { EMPTY } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';

import { TeamContext } from '../../../../core/team/team-context';
import {
  CreateTeamMemberRequest,
  Team,
  TeamMember,
  TeamMemberRole,
  UpdateTeamMemberRequest,
  UpdateTeamRequest,
} from '../../models/team.model';
import { TeamApi } from '../../services/team-api';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [
    NzAlertModule,
    NzEmptyModule,
    NzSpinModule,
    NzButtonModule,
    NzIconModule,
    ReactiveFormsModule,
    NzDrawerModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzPopconfirmModule,
  ],
  templateUrl: './team.html',
  styleUrl: './team.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPage {
  private readonly teamApi = inject(TeamApi);
  private readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);

  readonly loading = signal(false);
  readonly error = signal(false);
  readonly addMemberOpen = signal(false);
  readonly creatingMember = signal(false);

  readonly deletingMemberId = signal<string | null>(null);
  readonly team = signal<Team | null>(null);
  readonly members = signal<readonly TeamMember[]>([]);
  readonly editingMember = signal<TeamMember | null>(null);

  readonly memberRoles: readonly {
    value: TeamMemberRole;
    label: string;
  }[] = [
    { value: 'EngineeringManager', label: 'Engineering Manager' },
    { value: 'Developer', label: 'Developer' },
    { value: 'TechLead', label: 'Tech Lead' },
    { value: 'QA', label: 'QA' },
    { value: 'ProductManager', label: 'Product Manager' },
    { value: 'DataEngineer', label: 'Data Engineer' },
    { value: 'Other', label: 'Other' },
  ];

  readonly editTeamOpen = signal(false);
  readonly updatingTeam = signal(false);

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

  readonly memberMutationInProgress = () =>
    this.creatingMember() || this.deletingMemberId() !== null;

  constructor() {
    toObservable(this.teamContext.selectedTeamId)
      .pipe(
        tap((teamId) => {
          this.error.set(false);
          this.team.set(null);
          this.members.set([]);
          this.loading.set(!!teamId);
        }),

        switchMap((teamId) => {
          if (!teamId) {
            return EMPTY;
          }

          return this.teamApi.getTeam(teamId).pipe(
            switchMap((team) =>
              this.teamApi.getMembers(teamId).pipe(
                tap((members) => {
                  this.team.set(team);
                  this.members.set(members);
                  this.loading.set(false);
                }),
              ),
            ),

            catchError((error) => {
              this.message.error('Unable to update the team. Please try again.');

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

  roleLabel(role: TeamMember['role']): string {
    const labels: Record<TeamMember['role'], string> = {
      EngineeringManager: 'Engineering Manager',
      Developer: 'Developer',
      TechLead: 'Tech Lead',
      QA: 'QA',
      ProductManager: 'Product Manager',
      DataEngineer: 'Data Engineer',
      Other: 'Other',
    };

    return labels[role];
  }

  readonly addMemberForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),

    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(320)],
    }),

    role: new FormControl<TeamMemberRole>('Developer', {
      nonNullable: true,
      validators: [Validators.required],
    }),

    providerUserId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(200)],
    }),
  });

  openAddMember(): void {
    this.editingMember.set(null);

    this.addMemberForm.reset({
      name: '',
      email: '',
      role: 'Developer',
      providerUserId: '',
    });

    this.addMemberOpen.set(true);
  }

  closeAddMember(): void {
    if (this.creatingMember()) {
      return;
    }

    this.addMemberOpen.set(false);
    this.editingMember.set(null);
  }

  saveMember(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || this.addMemberForm.invalid) {
      this.addMemberForm.markAllAsTouched();
      return;
    }

    const value = this.addMemberForm.getRawValue();
    const providerUserId = value.providerUserId.trim();

    const request: CreateTeamMemberRequest | UpdateTeamMemberRequest = {
      name: value.name.trim(),
      email: value.email.trim(),
      role: value.role,
      providerUserId: providerUserId || null,
    };

    const editingMember = this.editingMember();

    this.creatingMember.set(true);

    const operation$ = editingMember
      ? this.teamApi.updateMember(teamId, editingMember.id, request)
      : this.teamApi.createMember(teamId, request);

    operation$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (savedMember) => {
        if (editingMember) {
          this.members.update((members) =>
            members.map((member) => (member.id === savedMember.id ? savedMember : member)),
          );
        } else {
          this.members.update((members) => [...members, savedMember]);
        }

        this.creatingMember.set(false);
        this.addMemberOpen.set(false);
        this.editingMember.set(null);

        this.addMemberForm.reset({
          name: '',
          email: '',
          role: 'Developer',
          providerUserId: '',
        });

        this.message.success(
          editingMember ? 'Team member updated successfully.' : 'Team member added successfully.',
        );
      },

      error: (error) => {
        this.message.error(
          editingMember ? 'Unable to update the team member.' : 'Unable to add the team member.',
        );

        this.creatingMember.set(false);
      },
    });
  }

  openEditMember(member: TeamMember): void {
    this.editingMember.set(member);

    this.addMemberForm.reset({
      name: member.name,
      email: member.email,
      role: member.role,
      providerUserId: member.providerUserId ?? '',
    });

    this.addMemberOpen.set(true);
  }

  deleteMember(member: TeamMember): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId) {
      return;
    }

    this.deletingMemberId.set(member.id);

    this.teamApi
      .deleteMember(teamId, member.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.members.update((members) => members.filter((current) => current.id !== member.id));

          this.deletingMemberId.set(null);

          this.message.success('Team member removed successfully.');
        },

        error: (error) => {
          this.message.error('Unable to remove the team member.');

          this.deletingMemberId.set(null);
        },
      });
  }

  openEditTeam(): void {
    const currentTeam = this.team();

    if (!currentTeam) {
      return;
    }

    this.editTeamForm.reset({
      name: currentTeam.name,
      description: currentTeam.description ?? '',
    });

    this.editTeamOpen.set(true);
  }

  closeEditTeam(): void {
    if (this.updatingTeam()) {
      return;
    }

    this.editTeamOpen.set(false);
  }

  updateTeam(): void {
    const teamId = this.teamContext.selectedTeamId();

    if (!teamId || this.editTeamForm.invalid) {
      this.editTeamForm.markAllAsTouched();
      return;
    }

    const value = this.editTeamForm.getRawValue();
    const description = value.description.trim();

    const request: UpdateTeamRequest = {
      name: value.name.trim(),
      description: description || null,
    };

    this.updatingTeam.set(true);

    this.teamApi
      .updateTeam(teamId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updatedTeam) => {
          this.team.set(updatedTeam);
          this.updatingTeam.set(false);
          this.editTeamOpen.set(false);
          this.message.success('Team updated successfully.');
        },
        error: (error) => {
          console.error('Failed to update team', error);
          this.updatingTeam.set(false);
        },
      });
  }
}
