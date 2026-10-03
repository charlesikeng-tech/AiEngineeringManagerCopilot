import { FormControl } from '@angular/forms';

import { microsoftTeamsWebhookUrl } from './microsoft-teams-webhook-url';

describe('Microsoft Teams webhook URL validation', () => {
  const workflow = '/workflows/workflow-id/triggers/manual/paths/invoke?sig=fake-signature';

  it.each([
    `https://example.environment.api.powerplatform.com/powerautomate/automations/direct${workflow}`,
    `https://example.logic.azure.com${workflow}`,
    `https://example.logic.azure.com:443${workflow}`,
    'https://example.logic.azure.com/workflows/id/triggers/manual/paths/invoke?sig=&sig=fake',
    'https://outlook.office.com/webhook/id/test',
    'https://example.webhook.office.com/webhookb2/id/test',
  ])('accepts supported webhook %s', (url) => {
    expect(microsoftTeamsWebhookUrl(new FormControl(url))).toBeNull();
  });

  it.each([
    '',
    'not-a-url',
    `http://example.logic.azure.com${workflow}`,
    `https://example.logic.azure.com:444${workflow}`,
    `https://user:password@example.logic.azure.com${workflow}`,
    `https://example.logic.azure.com${workflow}#fragment`,
    `https://example.logic.azure.com${workflow}#`,
    `https://example.logic.azure.com.evil.test${workflow}`,
    `https://logic.azure.com${workflow}`,
    `https://example.environment.api.powerplatform.com${workflow}`,
    'https://example.logic.azure.com/workflows/id/triggers/manual/paths/invoke',
    'https://example.logic.azure.com/workflows/id/triggers/manual/paths/invoke?sig=',
    'https://example.logic.azure.com/workflows/id/triggers/manual/paths/invoke/?sig=fake',
    'https://example.logic.azure.com/workflows/id/triggers/manual/paths/invoke?s%69g=fake',
    'https://example.logic.azure.com/other?sig=test',
    'https://example.logic.azure.com/workflows/extra/workflows/id/triggers/manual/paths/invoke?sig=test',
    'https://outlook.office.com.evil.test/webhook/test',
    'https://example.webhook.office.com/other/test',
    'https://outlook.office.com/webhook/test',
    'https://example.webhook.office.com/webhookb2/test',
    'https://hooks.slack.com/services/test',
  ])('rejects unsupported webhook %s', (url) => {
    expect(microsoftTeamsWebhookUrl(new FormControl(url))).toEqual({ microsoftTeamsWebhookUrl: true });
  });
});
