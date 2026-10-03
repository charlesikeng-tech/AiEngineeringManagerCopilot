import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, switchMap, timeout } from 'rxjs';

import { Auth } from '@core/auth/auth';
import { TeamApi } from '@core/team/team-api';
import { TeamContext } from '@core/team/team-context';

@Injectable({
  providedIn: 'root',
})
export class AppInitializer {
  private readonly auth = inject(Auth);
  private readonly teamApi = inject(TeamApi);
  private readonly teamContext = inject(TeamContext);
  readonly initializationError = signal<string | null>(null);

  initialize(): Observable<void> {
    this.initializationError.set(null);
    this.teamContext.clearTeam();
    return this.auth.loadCurrent().pipe(
      switchMap((user) => user ? this.initializeTeams() : of(undefined)),
      catchError(() => {
        this.initializationError.set('Impossible de charger votre session ou vos équipes. Vérifiez la connexion au serveur et rechargez la page.');
        return of(undefined);
      }),
    );
  }

  initializeTeams(): Observable<void> {
    this.initializationError.set(null);
    this.teamContext.clearTeam();
    return this.teamApi.getTeams().pipe(
      timeout(10000),
      map((teams) => {
        const selectedTeam = teams[0];
        if (selectedTeam) this.teamContext.selectTeam(selectedTeam.id);
      }),
    );
  }
}
