import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { I18nService, isAppLanguage } from '../i18n.service';

@Component({
  selector: 'app-language-selector',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './language-selector.html',
  styleUrl: './language-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSelector {
  readonly i18n = inject(I18nService);
  readonly loading = signal(false);
  readonly error = signal(false);

  async selectLanguage(select: HTMLSelectElement): Promise<void> {
    const language = select.value;
    if (!isAppLanguage(language)) {
      throw new Error(`Unsupported language selection: ${language}`);
    }

    this.loading.set(true);
    this.error.set(false);

    try {
      await this.i18n.setLanguage(language);
    } catch (error) {
      console.error('Unable to load the selected language.', error);
      this.error.set(true);
    } finally {
      select.value = this.i18n.language();
      this.loading.set(false);
    }
  }
}
