import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import managementEn from '../../../public/i18n/management/en.json';
import managementFr from '../../../public/i18n/management/fr.json';
import { ActionsApi } from './actions/services/actions-api';
import { Actions } from './actions/pages/actions/actions';
import { ReportAnalysisApi } from './reports/services/report-analysis-api';
import { ReportsApi } from './reports/services/reports-api';
import { ReportDetail } from './reports/pages/report-detail/report-detail';
import { Reports } from './reports/pages/reports/reports';
import { RisksApi } from './risks/services/risks-api';
import { Risks } from './risks/pages/risks/risks';
import { NzMessageService } from 'ng-zorro-antd/message';

describe('management page localization', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Actions, Reports, ReportDetail, Risks],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: managementEn, fr: managementFr }),
        { provide: TeamContext, useValue: { selectedTeamId: signal(null) } },
        { provide: ActionsApi, useValue: { getCurrentActions: () => ({}) } },
        { provide: ReportsApi, useValue: { getReports: () => ({}) } },
        {
          provide: ReportAnalysisApi,
          useValue: { getAnalysis: () => ({}) },
        },
        { provide: RisksApi, useValue: { getCurrentRisks: () => ({}) } },
        { provide: NzMessageService, useValue: { success: vi.fn(), error: vi.fn() } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } },
        },
      ],
    });
  });

  it('updates report, action, and risk labels when the language changes at runtime', async () => {
    const i18n = TestBed.inject(I18nService);
    await i18n.initialize();

    const actions = TestBed.createComponent(Actions).componentInstance;
    const reports = TestBed.createComponent(Reports).componentInstance;
    const reportDetail = TestBed.createComponent(ReportDetail).componentInstance;
    const risks = TestBed.createComponent(Risks).componentInstance;

    expect(actions.statusLabel('Todo')).toBe('Todo');
    expect(actions.metricLabel('CycleTime')).toBe('Cycle Time');
    expect(reports.healthLabel('Needs Attention')).toBe('Needs Attention');
    expect(reportDetail.categoryLabel('TechnicalDebt')).toBe('Technical debt');
    expect(risks.categoryLabel('Review')).toBe('Code review');

    await i18n.setLanguage('fr');

    expect(actions.statusLabel('Todo')).toBe('À faire');
    expect(actions.metricLabel('CycleTime')).toBe('Temps de cycle');
    expect(reports.healthLabel('Needs Attention')).toBe('À surveiller');
    expect(reportDetail.categoryLabel('TechnicalDebt')).toBe('Dette technique');
    expect(risks.categoryLabel('Review')).toBe('Revue de code');
    expect(risks.categoryLabel('CustomCategory')).toBe('CustomCategory');

    await i18n.setLanguage('en');
    expect(reportDetail.categoryLabel('TechnicalDebt')).toBe('Technical debt');
  });
});
