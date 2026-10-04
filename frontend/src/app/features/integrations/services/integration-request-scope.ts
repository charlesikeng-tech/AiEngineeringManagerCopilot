import { DestroyRef, effect, inject, Injectable, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TeamContext } from '@core/team/team-context';
import { EMPTY, Observable, Subject, catchError, filter, takeUntil, throwError } from 'rxjs';

/** One instance per widget: cancels work and rejects same-tick selection changes. */
@Injectable()
export class IntegrationRequestScope {
  readonly teamContext = inject(TeamContext);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changed = new Subject<void>();
  private generation = 0;

  watch(reset: () => void, load: (teamId: string) => void): void {
    this.destroyRef.onDestroy(reset);
    effect(() => {
      this.teamContext.selectionVersion();
      const teamId = this.teamContext.selectedTeamId();
      untracked(() => {
        this.generation++;
        this.changed.next();
        reset();
        if (teamId) load(teamId);
      });
    });
  }

  request<T>() {
    const generation = this.generation;
    const version = this.teamContext.selectionVersion();
    const teamId = this.teamContext.selectedTeamId();
    const current = () =>
      generation === this.generation &&
      version === this.teamContext.selectionVersion() &&
      teamId === this.teamContext.selectedTeamId();
    return (source: Observable<T>): Observable<T> =>
      source.pipe(
        takeUntil(this.changed),
        takeUntilDestroyed(this.destroyRef),
        filter(current),
        catchError((error: unknown) => (current() ? throwError(() => error) : EMPTY)),
      );
  }
}
