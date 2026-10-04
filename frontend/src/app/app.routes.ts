import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth-guard';
import { administratorGuard } from '@core/auth/administrator-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('@core/auth/sso-login').then(({ SsoLogin }) => SsoLogin),
  },
  {
    path: 'setup',
    data: { setup: true },
    loadComponent: () => import('@core/auth/administrator-access').then(({ AdministratorAccess }) => AdministratorAccess),
  },
  {
    path: 'admin/login',
    loadComponent: () => import('@core/auth/administrator-access').then(({ AdministratorAccess }) => AdministratorAccess),
  },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () =>
      import('@core/layout/main-layout/main-layout').then(({ MainLayout }) => MainLayout),
    children: [
      {
        path: 'admin/authentication',
        canActivate: [administratorGuard],
        loadComponent: () => import('@features/authentication/authentication-settings').then(({ AuthenticationSettings }) => AuthenticationSettings),
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('@features/dashboard/pages/dashboard/dashboard').then(
            ({ Dashboard }) => Dashboard,
          ),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('@features/reports/pages/reports/reports').then(({ Reports }) => Reports),
      },
      {
        path: 'reports/:reportId',
        loadComponent: () =>
          import('@features/reports/pages/report-detail/report-detail').then(
            ({ ReportDetail }) => ReportDetail,
          ),
      },
      {
        path: 'risks',
        loadComponent: () =>
          import('@features/risks/pages/risks/risks').then(({ Risks }) => Risks),
      },
      {
        path: 'actions',
        loadComponent: () =>
          import('@features/actions/pages/actions/actions').then(({ Actions }) => Actions),
      },
      {
        path: 'integrations',
        loadComponent: () =>
          import('@features/integrations/pages/integrations/integrations').then(
            ({ IntegrationsPage }) => IntegrationsPage,
          ),
      },
      {
        path: 'team',
        loadComponent: () =>
          import('@features/team/pages/teams/teams').then(({ TeamsPage }) => TeamsPage),
      },
      {
        path: 'team/:teamId',
        loadComponent: () =>
          import('@features/team/pages/team/team').then(({ TeamPage }) => TeamPage),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
