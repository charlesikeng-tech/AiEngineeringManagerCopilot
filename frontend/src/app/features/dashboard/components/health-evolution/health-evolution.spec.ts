import { TestBed } from '@angular/core/testing';
import { NGX_ECHARTS_CONFIG } from 'ngx-echarts';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import dashboardEn from '../../../../../../public/i18n/dashboard/en.json';
import dashboardFr from '../../../../../../public/i18n/dashboard/fr.json';

import { HealthEvolution } from './health-evolution';

describe('HealthEvolution', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HealthEvolution],
      providers: provideI18nTesting('en', { en: dashboardEn, fr: dashboardFr }),
    });
  });

  it('provides the chart loader locally without global ECharts configuration', async () => {
    const fixture = TestBed.createComponent(HealthEvolution);
    fixture.componentRef.setInput('history', []);
    fixture.detectChanges();

    const config = fixture.debugElement.injector.get(NGX_ECHARTS_CONFIG);
    expect(typeof config.echarts).toBe('function');

    const echarts = await config.echarts();
    expect(typeof echarts.init).toBe('function');
    expect(fixture.nativeElement.querySelector('nz-empty')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[echarts]')).toBeNull();
  });

  it('preserves chart options for the reporting history', () => {
    const fixture = TestBed.createComponent(HealthEvolution);
    fixture.componentRef.setInput('history', [
      {
        periodStart: '2026-09-01',
        periodEnd: '2026-09-30',
        overallScore: 80,
        healthLevel: 'Healthy',
        dataCoverage: 75,
      },
    ]);

    expect(fixture.componentInstance.chartOptions()).toEqual(
      expect.objectContaining({
        xAxis: expect.objectContaining({ data: ['Sep 2026'] }),
        series: [expect.objectContaining({ type: 'line', data: [80] })],
      }),
    );
  });

  it('updates chart labels and empty state when the language changes at runtime', async () => {
    const fixture = TestBed.createComponent(HealthEvolution);
    fixture.componentRef.setInput('history', [
      {
        periodStart: '2026-09-01',
        periodEnd: '2026-09-30',
        overallScore: 80,
        healthLevel: 'Healthy',
        dataCoverage: 75,
      },
    ]);

    expect(fixture.componentInstance.chartOptions()).toEqual(
      expect.objectContaining({
        series: [expect.objectContaining({ name: 'Health Score', data: [80] })],
      }),
    );

    await firstValueFrom(TestBed.inject(TranslateService).use('fr'));

    expect(fixture.componentInstance.chartOptions()).toEqual(
      expect.objectContaining({
        xAxis: expect.objectContaining({ data: ['sept. 2026'] }),
        series: [expect.objectContaining({ name: 'Score de santé', data: [80] })],
      }),
    );

    fixture.componentRef.setInput('history', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Évolution de la santé');
    expect(fixture.nativeElement.textContent).toContain('Aucun historique de santé disponible');
  });
});
