import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamContext } from '@core/team/team-context';
import { EngineeringMetric } from '@domains/engineering/models/metric';
import { of, Subject, throwError } from 'rxjs';
import { configureTeamTests, teamId, teamWidgetFixture } from '../../testing/team-testing';
import { TeamEngineeringData } from './team-engineering-data';

describe('team engineering data', () => {
  let mocks: ReturnType<typeof configureTeamTests>;
  const period = { periodStart: '2026-09-01', periodEnd: '2026-09-30' };
  const metric: EngineeringMetric = {
    id: 'metric-id',
    teamId,
    metricType: 'OpenPRs',
    value: 3,
    dataStatus: 'Available',
    ...period,
    createdAt: '2026-10-01T00:00:00Z',
  };
  beforeEach(() => {
    mocks = configureTeamTests(TeamEngineeringData);
  });
  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('calculates for the loaded team and does not generate a report for empty metrics', async () => {
    const widget = (await teamWidgetFixture(TeamEngineeringData)).componentInstance;
    widget.metricsPeriodForm.setValue(period);
    widget.calculateMetrics();
    expect(mocks.calculate).toHaveBeenCalledWith(teamId, period.periodStart, period.periodEnd);
    expect(widget.calculatingMetrics()).toBe(false);
    expect(widget.calculatedMetricsPeriod()).toEqual(period);
    widget.generateReport();
    expect(mocks.generateReport).not.toHaveBeenCalled();
    expect(mocks.api.getTeam).not.toHaveBeenCalled();
  });

  it('requires valid chronological dates and blocks repeated calculations while pending', async () => {
    const widget = (await teamWidgetFixture(TeamEngineeringData)).componentInstance;
    widget.metricsPeriodForm.setValue({ periodStart: '', periodEnd: '' });
    widget.calculateMetrics();
    expect(mocks.calculate).not.toHaveBeenCalled();
    expect(widget.metricsPeriodForm.controls.periodStart.touched).toBe(true);
    widget.metricsPeriodForm.setValue({ periodStart: '2026-09-30', periodEnd: '2026-09-01' });
    widget.calculateMetrics();
    expect(mocks.calculate).not.toHaveBeenCalled();
    expect(mocks.message.warning).toHaveBeenCalledWith(
      TestBed.inject(I18nService).t('team.notifications.invalidPeriod'),
    );
    mocks.calculate.mockReturnValueOnce(new Subject<EngineeringMetric[]>());
    widget.metricsPeriodForm.setValue(period);
    widget.calculateMetrics();
    widget.calculateMetrics();
    expect(mocks.calculate).toHaveBeenCalledTimes(1);
  });

  it('generates a report only for the calculated period and navigates on success', async () => {
    mocks.calculate.mockReturnValue(of([metric]));
    const fixture = await teamWidgetFixture(TeamEngineeringData);
    const widget = fixture.componentInstance;
    widget.metricsPeriodForm.setValue(period);
    widget.calculateMetrics();
    expect(widget.hasValidCalculatedMetrics()).toBe(true);
    expect(widget.availableMetricCount()).toBe(1);
    widget.metricsPeriodForm.controls.periodEnd.setValue('2026-10-01');
    widget.generateReport();
    expect(mocks.generateReport).not.toHaveBeenCalled();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      TestBed.inject(I18nService).t('team.periodChanged'),
    );
    widget.metricsPeriodForm.setValue(period);
    widget.generateReport();
    expect(mocks.generateReport).toHaveBeenCalledWith(teamId, period.periodStart, period.periodEnd);
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/reports', 'report-id']);
    expect(widget.generatingReport()).toBe(false);
  });

  it('keeps metric values, units and statuses localized without changing calculated data', async () => {
    mocks.calculate.mockReturnValue(of([metric]));
    const fixture = await teamWidgetFixture(TeamEngineeringData);
    const widget = fixture.componentInstance;
    widget.metricsPeriodForm.setValue(period);
    widget.calculateMetrics();
    expect(widget.metric('OpenPRs')).toEqual(metric);
    expect(widget.metric('CycleTime')).toBeNull();
    expect(widget.metricValue({ ...metric, value: null })).toBe('—');
    expect(widget.metricValue({ ...metric, dataStatus: 'NoData' })).toBe('—');
    expect(widget.metricValue({ ...metric, metricType: 'CycleTime' })).toContain('3');
    expect(widget.metricValue({ ...metric, metricType: 'ChangeFailureRate' })).toContain('%');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(widget.metricsPeriodForm.getRawValue()).toEqual(period);
    expect(widget.hasValidCalculatedMetrics()).toBe(true);
    expect(widget.metricLabel('OpenPRs')).toBe(TestBed.inject(I18nService).t('metrics.OpenPRs'));
    expect(widget.metricStatusLabel('NoData')).toBe(
      TestBed.inject(I18nService).t('team.metricStatuses.NoData'),
    );
  });

  it('renders calculation and report errors and supports retries', async () => {
    mocks.calculate
      .mockReturnValueOnce(throwError(() => new Error('unavailable')))
      .mockReturnValue(of([metric]));
    const fixture = await teamWidgetFixture(TeamEngineeringData);
    const widget = fixture.componentInstance;
    widget.metricsPeriodForm.setValue(period);
    widget.calculateMetrics();
    fixture.detectChanges();
    expect(widget.calculatingMetrics()).toBe(false);
    expect(widget.metricsError()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain(
      TestBed.inject(I18nService).t('team.metricsError'),
    );
    widget.calculateMetrics();
    expect(widget.metricsError()).toBe(false);
    mocks.generateReport.mockReturnValueOnce(throwError(() => new Error('unavailable')));
    widget.generateReport();
    fixture.detectChanges();
    expect(widget.generatingReport()).toBe(false);
    expect(widget.reportGenerationError()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain(
      TestBed.inject(I18nService).t('team.reportError'),
    );
    widget.generateReport();
    expect(widget.reportGenerationError()).toBe(false);
    expect(mocks.message.error).toHaveBeenCalledTimes(2);
  });

  it.each(['calculate', 'report'] as const)(
    'rejects same-tick stale %s results and cancels requests',
    async (action) => {
      mocks.calculate.mockReturnValue(of([metric]));
      const fixture = await teamWidgetFixture(TeamEngineeringData);
      const widget = fixture.componentInstance;
      widget.metricsPeriodForm.setValue(period);
      const pendingMetrics = new Subject<EngineeringMetric[]>();
      const pendingReport = new Subject<{ id: string }>();
      if (action === 'calculate') {
        mocks.calculate.mockReturnValue(pendingMetrics);
        widget.calculateMetrics();
      } else {
        widget.calculateMetrics();
        mocks.message.success.mockClear();
        mocks.generateReport.mockReturnValue(pendingReport);
        widget.generateReport();
      }
      const context = TestBed.inject(TeamContext);
      context.selectTeam('other-team');
      context.selectTeam(teamId);
      pendingMetrics.next([metric]);
      pendingReport.next({ id: 'stale-report' });
      expect(mocks.message.success).not.toHaveBeenCalled();
      expect(TestBed.inject(Router).navigate).not.toHaveBeenCalled();
      fixture.componentRef.setInput('selectionVersion', context.selectionVersion());
      fixture.detectChanges();
      await fixture.whenStable();
      expect(widget.metrics()).toEqual([]);
      expect(widget.calculatedMetricsPeriod()).toBeNull();
      expect(widget.calculatingMetrics()).toBe(false);
      expect(widget.generatingReport()).toBe(false);
      expect(pendingMetrics.observed).toBe(false);
      expect(pendingReport.observed).toBe(false);
      expect(widget.hasValidCalculatedMetrics()).toBe(false);
    },
  );
});
