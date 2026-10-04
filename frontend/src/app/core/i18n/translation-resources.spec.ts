import { mergeDeep } from '@ngx-translate/core';

import authenticationEn from '../../../../public/i18n/authentication/en.json';
import authenticationFr from '../../../../public/i18n/authentication/fr.json';
import accountEn from '../../../../public/i18n/account/en.json';
import accountFr from '../../../../public/i18n/account/fr.json';
import administrationEn from '../../../../public/i18n/administration/en.json';
import administrationFr from '../../../../public/i18n/administration/fr.json';
import commonEn from '../../../../public/i18n/common/en.json';
import commonFr from '../../../../public/i18n/common/fr.json';
import dashboardEn from '../../../../public/i18n/dashboard/en.json';
import dashboardFr from '../../../../public/i18n/dashboard/fr.json';
import managementEn from '../../../../public/i18n/management/en.json';
import managementFr from '../../../../public/i18n/management/fr.json';
import teamEn from '../../../../public/i18n/team/en.json';
import teamFr from '../../../../public/i18n/team/fr.json';

function flatten(value: unknown, prefix = ''): Record<string, string> {
  if (typeof value === 'string') {
    return { [prefix]: value };
  }
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    throw new Error(`Translation ${prefix} must be text or a namespace.`);
  }
  return Object.fromEntries(
    Object.entries(value).flatMap(([key, child]) =>
      Object.entries(flatten(child, prefix ? `${prefix}.${key}` : key)),
    ),
  );
}

function parameters(value: string): string[] {
  return [...value.matchAll(/{{\s*([\w.]+)\s*}}/g)].map((match) => match[1]).sort();
}

describe.each([
  { scope: 'common', en: commonEn, fr: commonFr },
  { scope: 'team', en: teamEn, fr: teamFr },
  { scope: 'dashboard', en: dashboardEn, fr: dashboardFr },
  { scope: 'management', en: managementEn, fr: managementFr },
  { scope: 'authentication', en: authenticationEn, fr: authenticationFr },
  { scope: 'account', en: accountEn, fr: accountFr },
  { scope: 'administration', en: administrationEn, fr: administrationFr },
])('$scope translations', ({ en, fr }) => {
  it('defines the same non-empty keys and interpolation parameters in both languages', () => {
    const english = flatten(en);
    const french = flatten(fr);

    expect(Object.keys(english).length).toBeGreaterThan(0);
    expect(Object.keys(french).sort()).toEqual(Object.keys(english).sort());

    for (const key of Object.keys(english)) {
      expect(english[key].trim(), key).not.toBe('');
      expect(french[key].trim(), key).not.toBe('');
      expect(parameters(french[key]), key).toEqual(parameters(english[key]));
    }
  });
});

describe.each([
  {
    language: 'en',
    resources: [authenticationEn, accountEn, administrationEn],
  },
  {
    language: 'fr',
    resources: [authenticationFr, accountFr, administrationFr],
  },
])(
  '$language authentication, account and administration boundaries',
  ({ resources }) => {
    it('owns each leaf key exactly once', () => {
      const keys = resources.flatMap((resource) => Object.keys(flatten(resource)));
      expect(new Set(keys).size).toBe(keys.length);
      expect(Object.keys(flatten(resources[0])).every((key) => key.startsWith('sso.login.'))).toBe(
        true,
      );
      expect(
        Object.keys(flatten(resources[1])).every((key) => key.startsWith('accountLink.')),
      ).toBe(true);
      expect(
        Object.keys(flatten(resources[2])).every(
          (key) => key.startsWith('sso.') && !key.startsWith('sso.login.'),
        ),
      ).toBe(true);
    });

    it('preserves every resource key and its exact text after deep merging', () => {
      const aggregate = resources.reduce(
        (dictionary, resource) => mergeDeep(dictionary, resource),
        {},
      );
      const expected = Object.assign({}, ...resources.map((resource) => flatten(resource)));
      expect(flatten(aggregate)).toEqual(expected);
    });
  },
);
