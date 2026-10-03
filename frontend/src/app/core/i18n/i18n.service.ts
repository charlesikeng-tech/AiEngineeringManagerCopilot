import { DOCUMENT, formatNumber, registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { enUS, fr } from 'date-fns/locale';
import { NzDateAdapter } from 'ng-zorro-antd/core/time';
import { en_US, fr_FR, NzI18nService } from 'ng-zorro-antd/i18n';
import { firstValueFrom } from 'rxjs';

registerLocaleData(localeFr);

export type AppLanguage = 'en' | 'fr';
export const LANGUAGE_STORAGE_KEY = 'em-copilot.language';

export function isAppLanguage(value: unknown): value is AppLanguage {
  return value === 'en' || value === 'fr';
}

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly translations = inject(TranslateService);
  private readonly nzI18n = inject(NzI18nService);
  private readonly dateAdapter = inject(NzDateAdapter);
  private readonly document = inject(DOCUMENT);
  private readonly languageState = signal<AppLanguage>('en');
  private languageRequest = 0;

  readonly language = this.languageState.asReadonly();
  readonly locale = computed(() => (this.language() === 'fr' ? 'fr-FR' : 'en-US'));

  constructor() {
    this.translations.onLangChange.pipe(takeUntilDestroyed()).subscribe(({ lang }) => {
      this.applyLanguage(lang);
    });

    const currentLanguage = this.translations.getCurrentLang();
    if (currentLanguage !== null) {
      this.applyLanguage(currentLanguage);
    }
  }

  async initialize(): Promise<void> {
    await firstValueFrom(this.translations.setFallbackLang('en'));
    await this.setLanguage(this.preferredLanguage());
  }

  async setLanguage(language: AppLanguage): Promise<void> {
    if (!isAppLanguage(language)) {
      throw new Error(`Unsupported application language: ${language}`);
    }

    const request = ++this.languageRequest;
    await firstValueFrom(this.translations.use(language));
    if (request === this.languageRequest) {
      this.persistLanguage(language);
    }
  }

  t(key: string, params?: Record<string, string | number>): string {
    this.language();
    const translation: unknown = this.translations.instant(key, params);

    if (typeof translation !== 'string') {
      throw new Error(`Expected a text translation for ${key}`);
    }

    return translation;
  }

  formatNumber(value: number, digitsInfo?: string): string {
    return formatNumber(value, this.locale(), digitsInfo);
  }

  private applyLanguage(language: string): void {
    if (!isAppLanguage(language)) {
      throw new Error(`Unsupported application language: ${language}`);
    }

    this.languageState.set(language);
    this.nzI18n.setLocale(language === 'fr' ? fr_FR : en_US);
    this.dateAdapter.setLocale(language === 'fr' ? fr : enUS);
    this.document.documentElement.lang = language;
    this.document.title = this.t('app.documentTitle');
  }

  private preferredLanguage(): AppLanguage {
    const browser = this.document.defaultView;
    let stored: string | null = null;

    try {
      stored = browser?.localStorage.getItem(LANGUAGE_STORAGE_KEY) ?? null;
    } catch (error) {
      if (!this.isUnavailableStorage(error)) {
        throw error;
      }
      console.warn('Language preferences cannot be read from browser storage.', error);
    }

    if (isAppLanguage(stored)) {
      return stored;
    }

    if (stored !== null) {
      console.warn(`Ignoring unsupported saved language: ${stored}`);
    }

    const browserLanguage = browser?.navigator.languages[0] ?? browser?.navigator.language;
    return browserLanguage?.toLowerCase().startsWith('fr') ? 'fr' : 'en';
  }

  private persistLanguage(language: AppLanguage): void {
    try {
      this.document.defaultView?.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
    } catch (error) {
      if (!this.isUnavailableStorage(error)) {
        throw error;
      }
      console.warn('Language preference cannot be saved in browser storage.', error);
    }
  }

  private isUnavailableStorage(error: unknown): boolean {
    return (
      error instanceof DOMException &&
      (error.name === 'SecurityError' || error.name === 'QuotaExceededError')
    );
  }
}
