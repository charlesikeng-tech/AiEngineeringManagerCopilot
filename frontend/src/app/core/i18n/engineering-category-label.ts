import { I18nService } from './i18n.service';

const engineeringCategories = new Set([
  'Delivery',
  'Quality',
  'Review',
  'Process',
  'Ownership',
  'TechnicalDebt',
  'Reliability',
]);

export function engineeringCategoryLabel(
  i18n: I18nService,
  category: string,
): string {
  return engineeringCategories.has(category)
    ? i18n.t(`risks.categories.${category}`)
    : category;
}
