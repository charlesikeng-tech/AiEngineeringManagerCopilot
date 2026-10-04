import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { TeamContext } from '@core/team/team-context';
import {
  AIAnalysis,
  EngineeringReport,
} from '@features/dashboard/models/engineering-dashboard-response';
import { BehaviorSubject, Observable, Subject, of, throwError } from 'rxjs';
import managementEn from '../../../../../../public/i18n/management/en.json';
import managementFr from '../../../../../../public/i18n/management/fr.json';
import { ReportsApi } from '../../services/reports-api';
import { ReportAnalysisApi } from '../../services/report-analysis-api';
import { ReportDetail } from './report-detail';

describe('Report notification deep links', () => {
  const oldTeam = '11111111-1111-1111-1111-111111111111';
  const linkedTeam = '22222222-2222-2222-2222-222222222222';
  const thirdTeam = '33333333-3333-3333-3333-333333333333';
  const firstReport = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const secondReport = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
  let params: BehaviorSubject<ParamMap>;
  let query: BehaviorSubject<ParamMap>;
  const getReport = vi.fn<(team: string, report: string) => Observable<EngineeringReport>>();
  const getAnalysis = vi.fn<() => Observable<AIAnalysis>>();
  const generateAnalysis = vi.fn<() => Observable<AIAnalysis>>();
  const report = (teamId: string, id: string): EngineeringReport => ({
    id,
    teamId,
    periodStart: '2026-09-01',
    periodEnd: '2026-09-30',
    executiveSummary: 'Summary',
    overallScore: 80,
    healthLevel: 'Healthy',
    dataCoverage: 100,
    createdAt: '2026-10-03T00:00:00Z',
    metrics: [],
    insights: [],
    risks: [],
    actions: [],
    trends: [],
  });

  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    params = new BehaviorSubject(convertToParamMap({ reportId: firstReport }));
    query = new BehaviorSubject(convertToParamMap({ teamId: linkedTeam }));
    getReport.mockReset().mockImplementation((team, id) => of(report(team, id)));
    getAnalysis
      .mockReset()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    generateAnalysis.mockReset();
    TestBed.configureTestingModule({
      imports: [ReportDetail],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: managementEn, fr: managementFr }),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: params.value, queryParamMap: query.value },
            paramMap: params.asObservable(),
            queryParamMap: query.asObservable(),
          },
        },
        { provide: ReportsApi, useValue: { getReport } },
        { provide: ReportAnalysisApi, useValue: { getAnalysis, generateAnalysis } },
      ],
    });
    TestBed.inject(TeamContext).selectTeam(oldTeam);
  });

  afterEach(() => localStorage.removeItem('selectedTeamId'));

  async function render() {
    const fixture = TestBed.createComponent(ReportDetail);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('selects the linked team before requesting its report', async () => {
    const fixture = await render();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(linkedTeam);
    expect(getReport).toHaveBeenCalledExactlyOnceWith(linkedTeam, firstReport);
    expect(fixture.componentInstance.report()?.teamId).toBe(linkedTeam);
    expect(getAnalysis).toHaveBeenCalledWith(linkedTeam, firstReport);
  });

  it('preserves ordinary report links without a team parameter', async () => {
    query.next(convertToParamMap({}));
    await render();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(oldTeam);
    expect(getReport).toHaveBeenCalledExactlyOnceWith(oldTeam, firstReport);
  });

  it('rejects invalid team parameters explicitly without changing selection', async () => {
    query.next(convertToParamMap({ teamId: 'invalid' }));
    const fixture = await render();
    expect(getReport).not.toHaveBeenCalled();
    expect(TestBed.inject(TeamContext).selectedTeamId()).toBe(oldTeam);
    expect(fixture.componentInstance.error()).toBe('reports.detailLoadError');
  });

  it('handles another report link when Angular reuses the component', async () => {
    const fixture = await render();
    params.next(convertToParamMap({ reportId: secondReport }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getReport).toHaveBeenLastCalledWith(linkedTeam, secondReport);
    expect(fixture.componentInstance.report()?.id).toBe(secondReport);
  });

  it('normalizes UUID casing before selecting the report team', async () => {
    query.next(convertToParamMap({ teamId: linkedTeam.toUpperCase() }));
    params.next(convertToParamMap({ reportId: firstReport.toUpperCase() }));
    await render();
    expect(getReport).toHaveBeenCalledExactlyOnceWith(linkedTeam, firstReport);
    expect(getAnalysis).toHaveBeenCalledWith(linkedTeam, firstReport);
  });

  it('ignores stale report responses while a different team link is being opened', async () => {
    const stale = new Subject<EngineeringReport>();
    getReport.mockReturnValueOnce(stale);
    const fixture = await render();
    query.next(convertToParamMap({ teamId: thirdTeam }));
    stale.next(report(linkedTeam, firstReport));
    expect(fixture.componentInstance.report()).toBeNull();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(stale.observed).toBe(false);
    expect(fixture.componentInstance.report()?.teamId).toBe(thirdTeam);
    expect(getAnalysis).not.toHaveBeenCalledWith(linkedTeam, firstReport);
  });

  it('cancels AI generation and rejects old results when the linked report changes', async () => {
    const fixture = await render();
    const pending = new Subject<AIAnalysis>();
    generateAnalysis.mockReturnValueOnce(pending);
    fixture.componentInstance.generateAnalysis();
    expect(pending.observed).toBe(true);
    params.next(convertToParamMap({ reportId: secondReport }));
    fixture.detectChanges();
    await fixture.whenStable();
    pending.next({
      summary: 'Old result',
      insights: [],
      actions: [],
      evidence: [],
      safeEvidence: [],
    });
    expect(pending.observed).toBe(false);
    expect(fixture.componentInstance.analysis()).toBeNull();
    expect(fixture.componentInstance.report()?.id).toBe(secondReport);
  });
});
