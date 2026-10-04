import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { I18nService } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { TeamMember, TeamMemberRole } from '@domains/teams/models/team-member';
import { TranslatePipe } from '@ngx-translate/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-team-member-editor',
  imports: [
    TranslatePipe,
    ReactiveFormsModule,
    NzButtonModule,
    NzDrawerModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
  ],
  templateUrl: './team-member-editor.html',
  host: { style: 'display: contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamMemberEditor {
  readonly teamId = input.required<string>();
  readonly selectionVersion = input.required<number>();
  readonly mutationBlocked = input(false);
  readonly saved = output<void>();
  private readonly teamApi = inject(TeamApi);
  private readonly context = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  private readonly i18n = inject(I18nService);
  private readonly changed = new Subject<void>();
  readonly addMemberOpen = signal(false);
  readonly creatingMember = signal(false);
  readonly editingMember = signal<TeamMember | null>(null);
  readonly memberRoles = [
    'EngineeringManager',
    'Developer',
    'TechLead',
    'QA',
    'ProductManager',
    'DataEngineer',
    'Other',
  ].map((value) => ({
    value: value as TeamMemberRole,
    label: `team.roles.${value}`,
  }));
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

  constructor() {
    effect(() => {
      this.teamId();
      this.selectionVersion();
      this.context.selectionVersion();
      this.changed.next();
      this.addMemberOpen.set(false);
      this.editingMember.set(null);
      this.creatingMember.set(false);
      this.resetForm();
    });
  }

  openAddMember(): void {
    if (!this.current()) return;
    this.editingMember.set(null);
    this.resetForm();
    this.addMemberOpen.set(true);
  }

  openEditMember(member: TeamMember): void {
    if (!this.current() || member.teamId !== this.teamId()) return;
    this.editingMember.set(member);
    this.addMemberForm.reset({
      name: member.name,
      email: member.email,
      role: member.role,
      providerUserId: member.providerUserId ?? '',
    });
    this.addMemberOpen.set(true);
  }

  closeAddMember(): void {
    if (this.creatingMember()) return;
    this.addMemberOpen.set(false);
    this.editingMember.set(null);
  }

  saveMember(): void {
    const teamId = this.teamId();
    const version = this.selectionVersion();
    if (
      !this.current(teamId, version) ||
      this.mutationBlocked() ||
      this.creatingMember() ||
      this.addMemberForm.invalid
    ) {
      this.addMemberForm.markAllAsTouched();
      return;
    }
    const value = this.addMemberForm.getRawValue();
    const request = {
      name: value.name.trim(),
      email: value.email.trim(),
      role: value.role,
      providerUserId: value.providerUserId.trim() || null,
    };
    const editingMember = this.editingMember();
    if (editingMember && editingMember.teamId !== teamId) return;
    this.creatingMember.set(true);
    const operation = editingMember
      ? this.teamApi.updateMember(teamId, editingMember.id, request)
      : this.teamApi.createMember(teamId, request);
    operation.pipe(takeUntil(this.changed), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (!this.current(teamId, version)) return;
        this.saved.emit();
        this.creatingMember.set(false);
        this.addMemberOpen.set(false);
        this.editingMember.set(null);
        this.resetForm();
        this.message.success(
          this.i18n.t(
            editingMember ? 'team.notifications.memberUpdated' : 'team.notifications.memberAdded',
          ),
        );
      },
      error: (error) => {
        if (!this.current(teamId, version)) return;
        console.error('Failed to save team member', error);
        this.message.error(
          this.i18n.t(
            editingMember
              ? 'team.notifications.memberUpdateFailed'
              : 'team.notifications.memberAddFailed',
          ),
        );
        this.creatingMember.set(false);
      },
    });
  }

  private current(teamId = this.teamId(), version = this.selectionVersion()): boolean {
    return (
      this.context.selectedTeamId() === teamId &&
      this.context.selectionVersion() === version &&
      this.teamId() === teamId &&
      this.selectionVersion() === version
    );
  }

  private resetForm(): void {
    this.addMemberForm.reset({ name: '', email: '', role: 'Developer', providerUserId: '' });
  }
}
