import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocalizedDatePipe } from '@core/i18n/localized-format.pipes';
import { Team } from '@domains/teams/models/team';
import { TeamApi } from '@domains/teams/data-access/team-api';
import { TeamContext } from '@core/team/team-context';
import { PaginationState } from '@shared/pagination/pagination-state';
import { TablePagination } from '@shared/ui/table-pagination/table-pagination';
import { TranslatePipe } from '@ngx-translate/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  defer,
  debounceTime,
  distinctUntilChanged,
  finalize,
  forkJoin,
  from,
  map,
  mergeMap,
  of,
  startWith,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { GitHubApi } from '@features/integrations/providers/github/data-access/github-api';
import { MicrosoftTeamsApi } from '@features/integrations/providers/microsoft-teams/data-access/microsoft-teams-api';
import { SlackApi } from '@features/integrations/providers/slack/data-access/slack-api';
import { IntegrationSettings } from '../../components/integration-settings/integration-settings';
import { JiraApi } from '@features/integrations/providers/jira/data-access/jira-api';

type ConnectionStatus = 'loading' | 'connected' | 'notConnected' | 'error';

interface ConnectionSummary {
  status: ConnectionStatus;
  lastSyncAt: string | null;
}

interface TeamIntegrationSummary {
  team: Team;
  github: ConnectionSummary;
  jira: ConnectionSummary;
  slack: ConnectionSummary;
  microsoftTeams: ConnectionSummary;
}

@Component({
  selector: 'app-integrations',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    TranslatePipe,
    LocalizedDatePipe,
    NzAlertModule,
    NzButtonModule,
    NzEmptyModule,
    NzInputModule,
    TablePagination,
    NzSpinModule,
    IntegrationSettings,
  ],
  templateUrl: './integrations.html',
  styleUrl: './integrations.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IntegrationsPage {
  private readonly teamApi = inject(TeamApi);
  private readonly githubApi = inject(GitHubApi);
  private readonly jiraApi = inject(JiraApi);
  private readonly slackApi = inject(SlackApi);
  private readonly microsoftTeamsApi = inject(MicrosoftTeamsApi);
  private readonly teamContext = inject(TeamContext);
  private readonly refreshRequests = new Subject<void>();
  private readonly searchChanges = new Subject<string>();

  readonly rows = signal<readonly TeamIntegrationSummary[]>([]);
  readonly loadingTeams = signal(true);
  readonly refreshing = signal(false);
  readonly loadError = signal(false);
  readonly searchText = signal('');
  readonly search = signal('');
  readonly pagination = new PaginationState();
  readonly pageNumber = this.pagination.pageNumber;
  readonly pageSize = this.pagination.pageSize;
  readonly totalCount = this.pagination.totalCount;
  readonly configuring = signal(false);
  readonly configuredTeamName = computed(
    () =>
      this.rows().find((row) => row.team.id === this.teamContext.selectedTeamId())?.team.name ?? '',
  );

  constructor() {
    this.searchChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((search) => {
        this.search.set(search);
        this.pageNumber.set(1);
        this.refresh();
      });
    this.refreshRequests
      .pipe(
        startWith(undefined),
        switchMap(() =>
          defer(() => {
            this.loadingTeams.set(true);
            this.refreshing.set(true);
            this.loadError.set(false);
            return this.teamApi
              .getTeamsPage(this.pageNumber(), this.pageSize(), this.search())
              .pipe(
                switchMap((page) => {
                  if (this.pagination.acceptTotal(page.totalCount)) {
                    return this.teamApi.getTeamsPage(
                      this.pageNumber(),
                      this.pageSize(),
                      this.search(),
                    );
                  }
                  return of(page);
                }),
                tap((page) => {
                  this.totalCount.set(page.totalCount);
                  const loading: ConnectionSummary = { status: 'loading', lastSyncAt: null };
                  this.rows.set(
                    page.items.map((team) => ({
                      team,
                      github: loading,
                      jira: loading,
                      slack: loading,
                      microsoftTeams: loading,
                    })),
                  );
                  this.loadingTeams.set(false);
                }),
                switchMap((page) =>
                  from(page.items).pipe(mergeMap((team) => this.loadSummary(team), 4)),
                ),
                tap((summary) =>
                  this.rows.update((rows) =>
                    rows.map((row) => (row.team.id === summary.team.id ? summary : row)),
                  ),
                ),
                catchError((error: unknown) => {
                  if (!(error instanceof HttpErrorResponse)) {
                    return throwError(() => error);
                  }
                  this.loadError.set(true);
                  return EMPTY;
                }),
                finalize(() => {
                  this.loadingTeams.set(false);
                  this.refreshing.set(false);
                }),
              );
          }),
        ),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  refresh(): void {
    this.refreshRequests.next();
  }

  setSearch(value: string): void {
    this.searchText.set(value);
    this.searchChanges.next(value.trim());
  }

  changePage(page: number): void {
    if (this.pagination.changePage(page)) {
      this.refresh();
    }
  }

  changePageSize(size: number): void {
    if (this.pagination.changePageSize(size)) {
      this.refresh();
    }
  }

  configure(teamId: string): void {
    this.teamContext.selectTeam(teamId);
    this.configuring.set(true);
  }

  showOverview(): void {
    this.configuring.set(false);
    this.refresh();
  }

  private loadSummary(team: Team): Observable<TeamIntegrationSummary> {
    return forkJoin({
      github: this.connectionSummary(
        this.githubApi.getConnection(team.id),
        (connection) => connection.lastSyncAt,
      ),
      jira: this.connectionSummary(
        this.jiraApi.getConnection(team.id),
        (connection) => connection.lastSyncAt,
      ),
      slack: this.connectionSummary(this.slackApi.getConnection(team.id)),
      microsoftTeams: this.connectionSummary(this.microsoftTeamsApi.getConnection(team.id)),
    }).pipe(map((connections) => ({ team, ...connections })));
  }

  private connectionSummary<T>(
    request: Observable<T>,
    lastSyncAt?: (connection: T) => string | null,
  ): Observable<ConnectionSummary> {
    return request.pipe(
      map((connection): ConnectionSummary => ({
        status: 'connected',
        lastSyncAt: lastSyncAt?.(connection) ?? null,
      })),
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse)) {
          return throwError(() => error);
        }
        return of<ConnectionSummary>({
          status: error.status === 404 ? 'notConnected' : 'error',
          lastSyncAt: null,
        });
      }),
    );
  }
}
