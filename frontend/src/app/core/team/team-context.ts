import { Injectable, computed, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TeamContext {
  private readonly selectedTeamIdState = signal<string | null>(null);

  readonly selectedTeamId = this.selectedTeamIdState.asReadonly();

  readonly hasSelectedTeam = computed(() => this.selectedTeamId() !== null);

  selectTeam(teamId: string): void {
    this.selectedTeamIdState.set(teamId);
  }

  clearTeam(): void {
    this.selectedTeamIdState.set(null);
  }
}
