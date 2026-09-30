import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./core/layout/main-layout/main-layout').then(({ MainLayout }) => MainLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard/dashboard').then(
            ({ Dashboard }) => Dashboard,
          ),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/pages/reports/reports').then(({ Reports }) => Reports),
      },
      {
        path: 'reports/:reportId',
        loadComponent: () =>
          import('./features/reports/pages/report-detail/report-detail').then(
            ({ ReportDetail }) => ReportDetail,
          ),
      },
      {
        path: 'risks',
        loadComponent: () =>
          import('./features/risks/pages/risks/risks').then(({ Risks }) => Risks),
      },
      {
        path: 'actions',
        loadComponent: () =>
          import('./features/actions/pages/actions/actions').then(({ Actions }) => Actions),
      },
      {
        path: 'team',
        loadComponent: () =>
          import('./features/team/pages/teams/teams').then(({ TeamsPage }) => TeamsPage),
      },
      {
        path: 'team/:teamId',
        loadComponent: () =>
          import('./features/team/pages/team/team').then(({ TeamPage }) => TeamPage),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
