import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';

const SELECTED_TEAM_STORAGE_KEY = 'selectedTeamId';

@Injectable({
  providedIn: 'root',
})
export class TeamContext {
  private readonly document = inject(DOCUMENT);
  private readonly selectedTeamIdState = signal<string | null>(this.readSelectedTeamId());

  readonly selectedTeamId = this.selectedTeamIdState.asReadonly();

  readonly hasSelectedTeam = computed(() => this.selectedTeamId() !== null);

  selectTeam(teamId: string): void {
    this.selectedTeamIdState.set(teamId);

    this.persistSelectedTeamId(teamId);
  }

  clearTeam(): void {
    this.selectedTeamIdState.set(null);

    this.removeSelectedTeamId();
  }

  private readSelectedTeamId(): string | null {
    try {
      return this.document.defaultView?.localStorage.getItem(SELECTED_TEAM_STORAGE_KEY) ?? null;
    } catch (error) {
      if (!this.isUnavailableStorage(error)) {
        throw error;
      }
      console.warn('Selected team cannot be read from browser storage.', error);
      return null;
    }
  }

  private persistSelectedTeamId(teamId: string): void {
    try {
      this.document.defaultView?.localStorage.setItem(SELECTED_TEAM_STORAGE_KEY, teamId);
    } catch (error) {
      if (!this.isUnavailableStorage(error)) {
        throw error;
      }
      console.warn('Selected team cannot be saved in browser storage.', error);
    }
  }

  private removeSelectedTeamId(): void {
    try {
      this.document.defaultView?.localStorage.removeItem(SELECTED_TEAM_STORAGE_KEY);
    } catch (error) {
      if (!this.isUnavailableStorage(error)) {
        throw error;
      }
      console.warn('Selected team cannot be removed from browser storage.', error);
    }
  }

  private isUnavailableStorage(error: unknown): boolean {
    return (
      error instanceof DOMException &&
      (error.name === 'SecurityError' || error.name === 'QuotaExceededError')
    );
  }
}
