import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { PagedResult } from '@core/models/paged-result';
import { TeamContext } from '@core/team/team-context';
import managementEn from '../../../../../../public/i18n/management/en.json';
import managementFr from '../../../../../../public/i18n/management/fr.json';
import { ReportHistoryItem } from '../../models/report-history-item';
import { ReportsApi } from '../../services/reports-api';
import { Reports } from './reports';

describe('Reports server pagination', () => {
  const report: ReportHistoryItem = {
    id: 'report-1',
    teamId: 'team-a',
    periodStart: '2026-09-01',
    periodEnd: '2026-09-30',
    overallScore: 75,
    healthLevel: 'Healthy',
    dataCoverage: 90,
    createdAt: '2026-10-01',
    scoreDelta: 8,
    coverageDelta: -5,
  };
  const getReports = vi.fn();
  const getReportsPage =
    vi.fn<(...args: [string, number, number]) => Observable<PagedResult<ReportHistoryItem>>>();
  const response = (items = [report], totalCount = 25): PagedResult<ReportHistoryItem> => ({
    items,
    totalCount,
    pageNumber: 1,
    pageSize: 10,
  });

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    vi.clearAllMocks();
    getReportsPage.mockReset().mockReturnValue(of(response()));
    TestBed.configureTestingModule({
      imports: [Reports],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: managementEn, fr: managementFr }),
        { provide: ReportsApi, useValue: { getReports, getReportsPage } },
      ],
    });
    TestBed.inject(TeamContext).selectTeam('team-a');
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  async function render() {
    const fixture = TestBed.createComponent(Reports);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  it('fetches only ten reports, renders a native shared table and full total', async () => {
    const fixture = await render();
    expect(getReportsPage).toHaveBeenCalledExactlyOnceWith('team-a', 1, 10);
    expect(getReports).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(
      fixture.nativeElement.querySelector('table.app-data-table caption').textContent,
    ).toContain('Report History');
    expect(fixture.nativeElement.querySelector('.page-summary__value').textContent).toContain('25');
    expect(fixture.nativeElement.textContent).toContain('Items per page');
    expect(fixture.nativeElement.textContent).toContain('of 25 items');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Éléments par page');
    expect(fixture.nativeElement.textContent).toContain('sur 25 éléments');
  });

  it('loads the next page lazily and resets the page on a size change', async () => {
    const page = (await render()).componentInstance;
    page.changePage(2);
    expect(getReportsPage).toHaveBeenLastCalledWith('team-a', 2, 10);
    page.changePageSize(20);
    expect(getReportsPage).toHaveBeenLastCalledWith('team-a', 1, 20);
    page.changePageSize(20);
    expect(getReportsPage).toHaveBeenCalledTimes(3);
  });

  it.each([
    ['Excellent', 'app-status-pill--success'],
    ['Healthy', 'app-status-pill--success'],
    ['Needs Attention', 'app-status-pill--warning'],
    ['At Risk', 'app-status-pill--warning'],
    ['Critical', 'app-status-pill--error'],
    ['No Data', 'app-status-pill--neutral'],
  ])('uses the shared badge for health level %s', async (healthLevel, modifier) => {
    getReportsPage.mockReturnValue(of(response([{ ...report, healthLevel }])));
    const fixture = await render();
    expect(
      fixture.nativeElement.querySelector('tbody .app-status-pill').classList.contains(modifier),
    ).toBe(true);
  });

  it('uses server deltas at page boundaries rather than recomputing from visible rows', async () => {
    const page = (await render()).componentInstance;
    expect(page.reportHistory()[0].scoreDelta).toBe(8);
    getReportsPage.mockReturnValueOnce(
      of(response([{ ...report, id: 'report-11', scoreDelta: -13, coverageDelta: 7 }])),
    );
    page.changePage(2);
    expect(page.reportHistory()[0].scoreDelta).toBe(-13);
    expect(page.reportHistory()[0].coverageDelta).toBe(7);
  });

  it('survives load errors and offers a localized retry', async () => {
    getReportsPage.mockReturnValueOnce(throwError(() => ({ status: 500 })));
    const fixture = await render();
    const retry: HTMLButtonElement = fixture.nativeElement.querySelector('.reports-error button');
    expect(retry.textContent).toContain('Retry');
    expect(fixture.componentInstance.error()).toBe('reports.loadError');
    retry.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.reports()).toEqual([report]);
    expect(fixture.componentInstance.error()).toBeNull();
    TestBed.inject(TeamContext).selectTeam('team-b');
    fixture.detectChanges();
    expect(getReportsPage).toHaveBeenLastCalledWith('team-b', 1, 10);
  });

  it('keeps unauthorized errors distinct from an empty no-report response', async () => {
    getReportsPage.mockReturnValueOnce(throwError(() => ({ status: 401 })));
    const page = (await render()).componentInstance;
    expect(page.error()).toBe('reports.loadError');
    getReportsPage.mockReturnValueOnce(throwError(() => ({ status: 404 })));
    page.retry();
    expect(page.error()).toBeNull();
    expect(page.reports()).toEqual([]);
  });

  it('retries a failed later page without resetting its requested page', async () => {
    const page = (await render()).componentInstance;
    getReportsPage.mockReturnValueOnce(throwError(() => ({ status: 500 })));
    page.changePage(2);
    expect(page.pagination.pageNumber()).toBe(2);
    page.retry();
    expect(getReportsPage).toHaveBeenLastCalledWith('team-a', 2, 10);
    expect(page.error()).toBeNull();
  });

  it('cancels in-flight requests when selection is cleared', async () => {
    const cancelled = vi.fn();
    getReportsPage.mockReturnValueOnce(new Observable(() => cancelled));
    const fixture = await render();
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    expect(cancelled).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.loading()).toBe(false);
    expect(fixture.componentInstance.pagination.totalCount()).toBe(0);
  });

  it('rejects callbacks synchronously across A→B→A and resets the page', async () => {
    const fixture = await render();
    const pending = new Subject<PagedResult<ReportHistoryItem>>();
    getReportsPage.mockReturnValueOnce(pending);
    fixture.componentInstance.changePage(2);
    const team = TestBed.inject(TeamContext);
    team.selectTeam('team-b');
    team.selectTeam('team-a');
    pending.next(response([{ ...report, id: 'obsolete' }], 99));
    expect(fixture.componentInstance.reports().some((item) => item.id === 'obsolete')).toBe(false);
    fixture.detectChanges();
    expect(getReportsPage).toHaveBeenLastCalledWith('team-a', 1, 10);
    expect(fixture.componentInstance.pagination.pageNumber()).toBe(1);
  });

  it('clamps and refetches if the last page disappeared', async () => {
    const page = (await render()).componentInstance;
    getReportsPage.mockReturnValueOnce(of(response([], 10)));
    page.changePage(3);
    expect(getReportsPage.mock.calls.slice(-2)).toEqual([
      ['team-a', 3, 10],
      ['team-a', 1, 10],
    ]);
    expect(page.reports()).toEqual([report]);
  });
});
