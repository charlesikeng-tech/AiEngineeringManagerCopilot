import { TestBed } from '@angular/core/testing';

import { provideI18nTesting } from '../i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '../i18n.service';
import { LanguageSelector } from './language-selector';

describe('LanguageSelector', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LanguageSelector],
      providers: provideI18nTesting(),
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('changes language from the header control without reloading the page', async () => {
    const fixture = TestBed.createComponent(LanguageSelector);
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('select');
    if (!(select instanceof HTMLSelectElement)) {
      throw new Error('Language control was not rendered.');
    }

    select.value = 'fr';
    select.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(I18nService).language()).toBe('fr');
    expect(fixture.nativeElement.querySelector('label').textContent).toContain('Langue');
    expect(select.value).toBe('fr');
    expect(select.disabled).toBe(false);
  });

  it('shows a load failure and restores the current selection', async () => {
    const fixture = TestBed.createComponent(LanguageSelector);
    fixture.detectChanges();
    const failure = new Error('Translations are unavailable.');
    vi.spyOn(TestBed.inject(I18nService), 'setLanguage').mockRejectedValue(failure);
    const log = vi.spyOn(console, 'error').mockImplementation(() => {});
    const select = fixture.nativeElement.querySelector('select');
    if (!(select instanceof HTMLSelectElement)) {
      throw new Error('Language control was not rendered.');
    }

    select.value = 'fr';
    await fixture.componentInstance.selectLanguage(select);
    fixture.detectChanges();

    expect(select.value).toBe('en');
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'Unable to load this language',
    );
    expect(select.disabled).toBe(false);
    expect(log).toHaveBeenCalledWith('Unable to load the selected language.', failure);
  });
});
