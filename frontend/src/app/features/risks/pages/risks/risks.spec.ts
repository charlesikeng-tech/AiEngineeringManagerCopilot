import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { EngineeringRisk } from '@features/dashboard/models/engineering-dashboard-response';
import managementEn from '../../../../../../public/i18n/management/en.json';
import managementFr from '../../../../../../public/i18n/management/fr.json';
import { PagedRisksResponse } from '../../models/paged-risks-response';
import { RisksApi } from '../../services/risks-api';
import { Risks } from './risks';

describe('Risks server pagination', () => {
  const risk: EngineeringRisk = {
    id: 'risk-1', reportId: 'report-1', metricType: 'CycleTime', severity: 'High',
    category: 'Delivery', title: 'Delivery risk', description: 'Detailed risk evidence',
    recommendation: 'Reduce bottlenecks', createdAt: '2026-10-01',
  };
  const getCurrentRisks = vi.fn();
  const getRisksPage = vi.fn<(...args: [string, number, number]) => Observable<PagedRisksResponse>>();
  const response = (items = [risk], totalCount = 25): PagedRisksResponse => ({
    teamId: 'team-a', reportId: 'report-1', periodStart: '2026-09-01', periodEnd: '2026-09-30',
    page: { items, totalCount, pageNumber: 1, pageSize: 10 },
    summary: { criticalCount: 3, highCount: 10, mediumCount: 8, lowCount: 4 },
  });

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    vi.clearAllMocks();
    getRisksPage.mockReset().mockReturnValue(of(response()));
    TestBed.configureTestingModule({
      imports: [Risks],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: managementEn, fr: managementFr }),
        { provide: RisksApi, useValue: { getCurrentRisks, getRisksPage } },
      ],
    });
    TestBed.inject(TeamContext).selectTeam('team-a');
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  async function render() {
    const fixture = TestBed.createComponent(Risks);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  it('loads the default page and preserves detailed cards with global summary counts', async () => {
    const fixture = await render();
    const page = fixture.componentInstance;
    expect(getRisksPage).toHaveBeenCalledExactlyOnceWith('team-a', 1, 10);
    expect(getCurrentRisks).not.toHaveBeenCalled();
    expect(page.totalCount()).toBe(25);
    expect([page.criticalCount(), page.highCount(), page.mediumCount(), page.lowCount()]).toEqual([3, 10, 8, 4]);
    expect(fixture.nativeElement.querySelectorAll('.risk-card')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain(risk.description);
    expect(fixture.nativeElement.textContent).toContain(risk.recommendation);
    expect(fixture.nativeElement.textContent).toContain('of 25 items');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Éléments par page');
  });

  it('fetches next pages lazily without re-sorting the server order and resets size', async () => {
    const page = (await render()).componentInstance;
    const low: EngineeringRisk = { ...risk, id: 'low', severity: 'Low' };
    getRisksPage.mockReturnValueOnce(of(response([low, risk])));
    page.changePage(2);
    expect(getRisksPage).toHaveBeenLastCalledWith('team-a', 2, 10);
    expect(page.sortedRisks().map((item) => item.id)).toEqual(['low', 'risk-1']);
    page.changePageSize(50);
    expect(getRisksPage).toHaveBeenLastCalledWith('team-a', 1, 50);
  });

  it('renders errors, then retries and remains live for subsequent team changes', async () => {
    getRisksPage.mockReturnValueOnce(throwError(() => ({ status: 500 })));
    const fixture = await render();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Retry');
    expect(fixture.componentInstance.error()).toBe(true);
    button.click();
    expect(fixture.componentInstance.risks()).toEqual([risk]);
    TestBed.inject(TeamContext).selectTeam('team-b');
    fixture.detectChanges();
    expect(getRisksPage).toHaveBeenLastCalledWith('team-b', 1, 10);
  });

  it('treats only no-report 404 as empty, not authorization failures', async () => {
    getRisksPage.mockReturnValueOnce(throwError(() => ({ status: 403 })));
    const page = (await render()).componentInstance;
    expect(page.error()).toBe(true);
    getRisksPage.mockReturnValueOnce(throwError(() => ({ status: 404 })));
    page.retry();
    expect(page.error()).toBe(false);
    expect(page.data()).toBeNull();
  });

  it('cancels requests even when team selection becomes empty', async () => {
    const cancelled = vi.fn();
    getRisksPage.mockReturnValueOnce(new Observable(() => cancelled));
    const fixture = await render();
    TestBed.inject(TeamContext).clearTeam();
    fixture.detectChanges();
    expect(cancelled).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.loading()).toBe(false);
    expect(fixture.componentInstance.data()).toBeNull();
  });

  it('rejects stale callbacks before effects run, including an A→B→A switch', async () => {
    const fixture = await render();
    const pending = new Subject<PagedRisksResponse>();
    getRisksPage.mockReturnValueOnce(pending);
    fixture.componentInstance.changePage(2);
    const team = TestBed.inject(TeamContext);
    team.selectTeam('team-b');
    team.selectTeam('team-a');
    pending.next(response([{ ...risk, id: 'obsolete' }], 99));
    expect(fixture.componentInstance.risks().some((item) => item.id === 'obsolete')).toBe(false);
    fixture.detectChanges();
    expect(getRisksPage).toHaveBeenLastCalledWith('team-a', 1, 10);
  });

  it('refetches a valid last page after totals shrink', async () => {
    const page = (await render()).componentInstance;
    getRisksPage.mockReturnValueOnce(of(response([], 10)));
    page.changePage(3);
    expect(getRisksPage.mock.calls.slice(-2)).toEqual([['team-a', 3, 10], ['team-a', 1, 10]]);
    expect(page.risks()).toEqual([risk]);
  });
});
