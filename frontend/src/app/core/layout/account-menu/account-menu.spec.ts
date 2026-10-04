import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import {
  DownOutline,
  KeyOutline,
  LogoutOutline,
  SafetyCertificateOutline,
} from '@ant-design/icons-angular/icons';

import { Auth } from '@core/auth/auth';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { TeamContext } from '@core/team/team-context';

import { provideNzIcons } from 'ng-zorro-antd/icon';

import { of } from 'rxjs';

import { AccountMenu } from './account-menu';
import authenticationEn from '../../../../../public/i18n/authentication/en.json';
import authenticationFr from '../../../../../public/i18n/authentication/fr.json';
import accountEn from '../../../../../public/i18n/account/en.json';
import accountFr from '../../../../../public/i18n/account/fr.json';
import administrationEn from '../../../../../public/i18n/administration/en.json';
import administrationFr from '../../../../../public/i18n/administration/fr.json';

describe('AccountMenu', () => {
  const auth = {
    user: () => ({
      id: 'admin-id',
      name: 'Charles Ikeng',
      email: 'charles@example.com',
      role: 'PlatformAdministrator' as const,
    }),

    logout: vi.fn(() => of(undefined)),
  };

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      imports: [AccountMenu],

      providers: [
        provideRouter([]),

        ...provideI18nTesting('en', {
          en: { ...accountEn, sso: { ...authenticationEn.sso, ...administrationEn.sso } },
          fr: { ...accountFr, sso: { ...authenticationFr.sso, ...administrationFr.sso } },
        }),

        provideNzIcons([DownOutline, KeyOutline, LogoutOutline, SafetyCertificateOutline]),

        {
          provide: Auth,
          useValue: auth,
        },
      ],
    });
  });

  it('displays the current user identity', () => {
    const fixture = TestBed.createComponent(AccountMenu);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Charles Ikeng');

    expect(fixture.componentInstance.initials()).toBe('CI');

    expect(fixture.componentInstance.roleLabel()).toBe('Administrator');
  });

  it('clears team context and returns to login on logout', () => {
    const teamContext = TestBed.inject(TeamContext);

    const clear = vi.spyOn(teamContext, 'clearTeam');

    const router = TestBed.inject(Router);

    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    const component = TestBed.createComponent(AccountMenu).componentInstance;

    component.logout();

    expect(auth.logout).toHaveBeenCalledOnce();

    expect(clear).toHaveBeenCalledOnce();

    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
