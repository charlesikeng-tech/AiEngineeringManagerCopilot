import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import dashboardEn from '../../../../../../public/i18n/dashboard/en.json';
import dashboardFr from '../../../../../../public/i18n/dashboard/fr.json';
import type { EngineeringRisk } from '@domains/engineering/models/risk';
import { DashboardRisks } from './dashboard-risks';

describe('DashboardRisks summary', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      imports: [DashboardRisks],
      providers: [
        provideRouter([]),
        ...provideI18nTesting('en', { en: dashboardEn, fr: dashboardFr }),
      ],
    }),
  );

  it('keeps the dashboard bounded and links to the complete paginated list', async () => {
    const risks: EngineeringRisk[] = Array.from({ length: 12 }, (_, index) => ({
      id: `risk-${index}`,
      reportId: 'report',
      metricType: null,
      category: 'Delivery',
      severity: index === 11 ? 'Critical' : 'Low',
      title: `Risk ${index}`,
      description: 'Description',
      recommendation: 'Recommendation',
      createdAt: '2026-10-03T00:00:00Z',
    }));
    const fixture = TestBed.createComponent(DashboardRisks);
    fixture.componentRef.setInput('risks', risks);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelectorAll('.risk-item')).toHaveLength(5);
    expect(fixture.nativeElement.querySelector('.risk-item').textContent).toContain('Risk 11');
    expect(fixture.nativeElement.querySelector('nz-tag').textContent).toContain('12');
    expect(fixture.nativeElement.querySelector('a[routerlink="/risks"]').textContent).toContain(
      'View all risks',
    );
    expect(risks[0].id).toBe('risk-0');
  });

  it('does not show a redundant navigation link for a small summary', async () => {
    const fixture = TestBed.createComponent(DashboardRisks);
    fixture.componentRef.setInput('risks', []);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('a[routerlink="/risks"]')).toBeNull();
  });
});
