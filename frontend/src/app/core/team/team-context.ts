import { Injectable, computed, signal } from '@angular/core';

const SELECTED_TEAM_STORAGE_KEY = 'selectedTeamId';

@Injectable({
  providedIn: 'root',
})
export class TeamContext {
  private readonly selectedTeamIdState = signal<string | null>(
    localStorage.getItem(SELECTED_TEAM_STORAGE_KEY),
  );

  readonly selectedTeamId = this.selectedTeamIdState.asReadonly();

  readonly hasSelectedTeam = computed(() => this.selectedTeamId() !== null);

  selectTeam(teamId: string): void {
    this.selectedTeamIdState.set(teamId);

    localStorage.setItem(SELECTED_TEAM_STORAGE_KEY, teamId);
  }

  clearTeam(): void {
    this.selectedTeamIdState.set(null);

    localStorage.removeItem(SELECTED_TEAM_STORAGE_KEY);
  }
}
