import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import {
  ApiOutline,
  CheckSquareOutline,
  DashboardOutline,
  FileTextOutline,
  LockOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  TeamOutline,
  WarningOutline,
} from '@ant-design/icons-angular/icons';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { TeamApi } from '@core/team/team-api';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { of } from 'rxjs';
import { routes } from '../../../app.routes';
import { MainLayout } from './main-layout';
import { Auth } from '@core/auth/auth';
import ssoEn from '../../../../../public/i18n/sso/en.json';
import ssoFr from '../../../../../public/i18n/sso/fr.json';

@Component({ template: '' })
class EmptyPage {}

describe('MainLayout integrations navigation', () => {
  beforeEach(() => {
    localStorage.removeItem('selectedTeamId');
    TestBed.configureTestingModule({
      imports: [MainLayout],
      providers: [
        provideRouter([{ path: 'integrations', component: EmptyPage }]),
        ...provideI18nTesting('en', { en: ssoEn, fr: ssoFr }),
        provideNzIcons([
          ApiOutline,
          CheckSquareOutline,
          DashboardOutline,
          FileTextOutline,
          LockOutline,
          MenuFoldOutline,
          MenuUnfoldOutline,
          TeamOutline,
          WarningOutline,
        ]),
        { provide: TeamApi, useValue: { getTeams: () => of([]) } },
        { provide: Auth, useValue: { logout: () => of(undefined), user: () => ({ role: 'PlatformAdministrator' }) } },
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem('selectedTeamId');
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('registers the integrations route under the main layout', () => {
    expect(
      routes.find((route) => route.path === '')?.children?.find((route) => route.path === 'integrations')?.loadComponent,
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

  it('provides a localized administrator authentication navigation entry and guarded route', async () => {
    const route = routes.find((route) => route.path === '')?.children?.find((route) => route.path === 'admin/authentication');
    expect(route?.canActivate?.length).toBe(1);
    const fixture = TestBed.createComponent(MainLayout);
    fixture.detectChanges();
    const item = fixture.nativeElement.querySelector('li[routerlink="/admin/authentication"]');
    expect(item.textContent).toContain('Administrator authentication');
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(item.textContent).toContain('Authentification administrateur');
  });
});
