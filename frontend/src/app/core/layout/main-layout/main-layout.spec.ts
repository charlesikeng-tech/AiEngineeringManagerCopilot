import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import {
  ApiOutline,
  CheckSquareOutline,
  DashboardOutline,
  DownOutline,
  FileTextOutline,
  KeyOutline,
  LockOutline,
  LogoutOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  SafetyCertificateOutline,
  TeamOutline,
  WarningOutline,
} from '@ant-design/icons-angular/icons';

import { Auth } from '@core/auth/auth';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamApi } from '@core/team/team-api';

import { provideNzIcons } from 'ng-zorro-antd/icon';

import { of } from 'rxjs';

import { routes } from '../../../app.routes';

import { MainLayout } from './main-layout';

import ssoEn from '../../../../../public/i18n/sso/en.json';
import ssoFr from '../../../../../public/i18n/sso/fr.json';

@Component({
  standalone: true,
  template: '',
})
class EmptyPage {}

describe('MainLayout integrations navigation', () => {
  let role: 'User' | 'PlatformAdministrator';

  const auth = {
    logout: vi.fn(() => of(undefined)),

    user: () => ({
      id: 'user-id',
      name: 'Charles Ikeng',
      email: 'charles@example.com',
      role,
    }),
  };

  beforeEach(() => {
    vi.clearAllMocks();

    role = 'PlatformAdministrator';

    localStorage.removeItem('selectedTeamId');

    TestBed.configureTestingModule({
      imports: [MainLayout],

      providers: [
        provideRouter([
          {
            path: 'integrations',
            component: EmptyPage,
          },
        ]),

        ...provideI18nTesting('en', {
          en: ssoEn,
          fr: ssoFr,
        }),

        provideNzIcons([
          ApiOutline,
          CheckSquareOutline,
          DashboardOutline,
          DownOutline,
          FileTextOutline,
          KeyOutline,
          LockOutline,
          LogoutOutline,
          MenuFoldOutline,
          MenuUnfoldOutline,
          SafetyCertificateOutline,
          TeamOutline,
          WarningOutline,
        ]),

        {
          provide: TeamApi,
          useValue: {
            getTeams: () => of([]),
          },
        },

        {
          provide: Auth,
          useValue: auth,
        },
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('registers the integrations route under the main layout', () => {
    expect(
      routes
        .find((route) => route.path === '')
        ?.children?.find((route) => route.path === 'integrations')?.loadComponent,
    ).toBeDefined();
  });

  it('provides a localized active menu entry for the integrations page', async () => {
    const fixture = TestBed.createComponent(MainLayout);

    fixture.detectChanges();

    await TestBed.inject(Router).navigateByUrl('/integrations');

    await fixture.whenStable();

    fixture.detectChanges();

    const item = fixture.nativeElement.querySelector('li[routerlink="/integrations"]');

    expect(item).not.toBeNull();

    expect(item.textContent).toContain('Integrations');

    expect(item.classList.contains('ant-menu-item-selected')).toBe(true);

    await TestBed.inject(I18nService).setLanguage('fr');

    fixture.detectChanges();

    expect(item.textContent).toContain('Intégrations');
  });

  it('provides localized administrator navigation entries', async () => {
    const route = routes
      .find((candidate) => candidate.path === '')
      ?.children?.find((candidate) => candidate.path === 'admin/authentication');

    expect(route?.canActivate?.length).toBe(1);

    const fixture = TestBed.createComponent(MainLayout);

    fixture.detectChanges();

    const authentication = fixture.nativeElement.querySelector(
      'li[routerlink="/admin/authentication"]',
    );

    const security = fixture.nativeElement.querySelector('li[routerlink="/account/security"]');

    expect(authentication.textContent).toContain('Administrator authentication');

    expect(security.textContent).toContain('Account security');

    await TestBed.inject(I18nService).setLanguage('fr');

    fixture.detectChanges();

    expect(authentication.textContent).toContain('Authentification administrateur');

    expect(security.textContent).toContain('Sécurité du compte');
  });

  it('never shows provider administration to an ordinary SSO user', () => {
    role = 'User';

    const fixture = TestBed.createComponent(MainLayout);

    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('li[routerlink="/admin/authentication"]'),
    ).toBeNull();

    expect(fixture.nativeElement.querySelector('li[routerlink="/account/security"]')).toBeNull();
  });

  it('displays the authenticated account in the topbar', () => {
    const fixture = TestBed.createComponent(MainLayout);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Charles Ikeng');
  });
});
