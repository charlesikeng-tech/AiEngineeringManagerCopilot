import { TestBed } from '@angular/core/testing';

import managementEn from '../../../../public/i18n/management/en.json';
import managementFr from '../../../../public/i18n/management/fr.json';
import { provideI18nTesting } from './i18n-testing';
import { I18nService } from './i18n.service';
import { engineeringCategoryLabel } from './engineering-category-label';

describe('engineeringCategoryLabel', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ...provideI18nTesting('en', {
          en: managementEn,
          fr: managementFr,
        }),
      ],
    });
  });

  it('translates known categories and preserves unknown values', async () => {
    const i18n = TestBed.inject(I18nService);

    expect(engineeringCategoryLabel(i18n, 'TechnicalDebt')).toBe('Technical debt');
    await i18n.setLanguage('fr');
    expect(engineeringCategoryLabel(i18n, 'TechnicalDebt')).toBe('Dette technique');
    expect(engineeringCategoryLabel(i18n, 'Custom category')).toBe('Custom category');
  });
});
