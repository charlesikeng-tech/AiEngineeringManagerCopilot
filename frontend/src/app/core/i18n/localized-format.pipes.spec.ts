import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

import { provideI18nTesting } from './i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from './i18n.service';
import {
  LocalizedDatePipe,
  LocalizedNumberPipe,
  LocalizedPercentPipe,
} from './localized-format.pipes';

@Component({
  standalone: true,
  imports: [
    TranslatePipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    LocalizedPercentPipe,
    ReactiveFormsModule,
  ],
  template: `
    <span class="text">{{ 'app.reports' | translate }}</span>
    <span class="date">{{ date | appDate: 'MMMM' : 'UTC' }}</span>
    <span class="number">{{ number | appNumber: '1.1-1' }}</span>
    <span class="percent">{{ percent | appPercent: '1.1-1' }}</span>
    <input [formControl]="name" [placeholder]="'app.selectTeam' | translate" />
  `,
})
class LocalizedView {
  readonly date = new Date('2026-09-01T00:00:00Z');
  readonly number = 1234.5;
  readonly percent = 0.125;
  readonly name = new FormControl('charlesikeng-tech', { nonNullable: true });
}

describe('localized format pipes', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LocalizedView],
      providers: provideI18nTesting(),
    });
  });

  afterEach(() => {
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('updates existing text, dates and numbers without changing input values', async () => {
    const fixture = TestBed.createComponent(LocalizedView);
    const service = TestBed.inject(I18nService);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.text').textContent).toBe('Reports');
    expect(fixture.nativeElement.querySelector('.date').textContent).toBe('September');
    expect(fixture.nativeElement.querySelector('.number').textContent).toBe('1,234.5');
    expect(fixture.nativeElement.querySelector('.percent').textContent).toBe('12.5%');

    await service.setLanguage('fr');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.text').textContent).toBe('Rapports');
    expect(fixture.nativeElement.querySelector('.date').textContent).toBe('septembre');
    expect(fixture.nativeElement.querySelector('.number').textContent).toContain('234,5');
    expect(fixture.nativeElement.querySelector('.percent').textContent).toContain('12,5');
    expect(fixture.componentInstance.number).toBe(1234.5);
    expect(fixture.componentInstance.name.value).toBe('charlesikeng-tech');
    expect(fixture.nativeElement.querySelector('input').placeholder).toBe(
      'Sélectionner une équipe',
    );
  });

  it('preserves empty-value handling', () => {
    const date = TestBed.runInInjectionContext(() => new LocalizedDatePipe());
    const number = TestBed.runInInjectionContext(() => new LocalizedNumberPipe());
    const percent = TestBed.runInInjectionContext(() => new LocalizedPercentPipe());

    expect(date.transform(null)).toBeNull();
    expect(number.transform(undefined)).toBeNull();
    expect(percent.transform(null)).toBeNull();
  });
});
