import { inject, Injectable } from '@angular/core';
import { map, Observable, of, switchMap } from 'rxjs';

import { Auth } from '../auth/auth';
import { TeamApi } from '../team/team-api';
import { TeamContext } from '../team/team-context';
import { DevelopmentBootstrap } from './development-bootstrap';

@Injectable({
  providedIn: 'root',
})
export class AppInitializer {
  private readonly auth = inject(Auth);
  private readonly teamApi = inject(TeamApi);
  private readonly teamContext = inject(TeamContext);
  private readonly developmentBootstrap = inject(DevelopmentBootstrap);

  initialize(): Observable<void> {
    return this.auth.authenticateForDevelopment().pipe(
      switchMap(() => this.teamApi.getTeams()),

      switchMap((teams) => {
        const selectedTeam = teams[0];

        if (!selectedTeam) {
          return of(undefined);
        }

        this.teamContext.selectTeam(selectedTeam.id);

        // Ensure August report exists first
        return this.developmentBootstrap
          .ensureReport(selectedTeam.id, '2026-08-01', '2026-08-31')
          .pipe(
            // Then ensure September report exists
            switchMap(() =>
              this.developmentBootstrap.ensureReport(selectedTeam.id, '2026-09-01', '2026-09-30'),
            ),

            // AI analysis only for the current/latest report
            switchMap((septemberReport) =>
              this.developmentBootstrap.ensureAnalysis(selectedTeam.id, septemberReport.id),
            ),

            map(() => undefined),
          );
      }),
    );
  }
}
