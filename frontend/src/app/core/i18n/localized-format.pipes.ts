import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { inject, Pipe, PipeTransform } from '@angular/core';

import { I18nService } from './i18n.service';

class FormatCache {
  private previousArgs: readonly unknown[] | null = null;
  private previousResult: string | null = null;

  get(args: readonly unknown[], format: () => string | null): string | null {
    if (this.previousArgs?.every((arg, index) => arg === args[index])) {
      return this.previousResult;
    }

    const result = format();
    this.previousArgs = args;
    this.previousResult = result;
    return result;
  }
}

// Locale changes must update the output even when the value is unchanged.
@Pipe({ name: 'appDate', standalone: true, pure: false })
export class LocalizedDatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);
  private readonly pipe = new DatePipe('en-US');
  private readonly cache = new FormatCache();

  transform(
    value: Date | string | number | null | undefined,
    format?: string,
    timezone?: string,
  ): string | null {
    const locale = this.i18n.locale();
    return this.cache.get([value, format, timezone, locale], () =>
      this.pipe.transform(value, format, timezone, locale),
    );
  }
}

@Pipe({ name: 'appNumber', standalone: true, pure: false })
export class LocalizedNumberPipe implements PipeTransform {
  private readonly i18n = inject(I18nService);
  private readonly pipe = new DecimalPipe('en-US');
  private readonly cache = new FormatCache();

  transform(value: number | string | null | undefined, digitsInfo?: string): string | null {
    const locale = this.i18n.locale();
    return this.cache.get([value, digitsInfo, locale], () =>
      this.pipe.transform(value, digitsInfo, locale),
    );
  }
}

@Pipe({ name: 'appPercent', standalone: true, pure: false })
export class LocalizedPercentPipe implements PipeTransform {
  private readonly i18n = inject(I18nService);
  private readonly pipe = new PercentPipe('en-US');
  private readonly cache = new FormatCache();

  transform(value: number | string | null | undefined, digitsInfo?: string): string | null {
    const locale = this.i18n.locale();
    return this.cache.get([value, digitsInfo, locale], () =>
      this.pipe.transform(value, digitsInfo, locale),
    );
  }
}
