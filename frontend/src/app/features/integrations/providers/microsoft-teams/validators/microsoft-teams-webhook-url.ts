import { AbstractControl, ValidationErrors } from '@angular/forms';

export function microsoftTeamsWebhookUrl(control: AbstractControl): ValidationErrors | null {
  try {
    const url = new URL(control.value);
    if (
      url.protocol !== 'https:' ||
      url.username ||
      url.password ||
      url.href.includes('#') ||
      (url.port && url.port !== '443')
    ) {
      return { microsoftTeamsWebhookUrl: true };
    }

    const isWorkflow =
      (url.hostname.endsWith('.environment.api.powerplatform.com') &&
        /^\/powerautomate\/automations\/direct\/workflows\/[^/]+\/triggers\/manual\/paths\/invoke$/.test(url.pathname)) ||
      (url.hostname.endsWith('.logic.azure.com') &&
        /^\/workflows\/[^/]+\/triggers\/manual\/paths\/invoke$/.test(url.pathname));
    const isLegacy =
      (url.hostname === 'outlook.office.com' || url.hostname.endsWith('.webhook.office.com')) &&
      /^\/webhook(?:b2)?\/[^/]+\/.+/.test(url.pathname);

    const hasSignature = url.search.slice(1).split('&')
      .some((parameter) => parameter.startsWith('sig=') && parameter.length > 4);

    return (isWorkflow && hasSignature) || isLegacy
      ? null
      : { microsoftTeamsWebhookUrl: true };
  } catch {
    return { microsoftTeamsWebhookUrl: true };
  }
}
