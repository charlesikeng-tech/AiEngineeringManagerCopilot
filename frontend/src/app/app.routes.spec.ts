import { administratorGuard } from '@core/auth/administrator-guard';
import { authGuard } from '@core/auth/auth-guard';
import { AccountSecurity } from '@features/account/pages/security/account-security';
import { AuthenticationSettings } from '@features/administration/pages/authentication-settings/authentication-settings';
import { AdministratorAccess } from '@features/authentication/pages/administrator-access/administrator-access';
import { SsoLogin } from '@features/authentication/pages/login/sso-login';
import { routes } from './app.routes';

describe('authentication routes', () => {
  it('loads the public login page without session guards', async () => {
    const login = routes.find((route) => route.path === 'login')!;

    expect(await login.loadComponent!()).toBe(SsoLogin);
    expect(login.canActivate).toBeUndefined();
  });

  it('shares administrator access between setup and local login with setup data only on setup', async () => {
    const setup = routes.find((route) => route.path === 'setup')!;
    const login = routes.find((route) => route.path === 'admin/login')!;

    expect(await setup.loadComponent!()).toBe(AdministratorAccess);
    expect(await login.loadComponent!()).toBe(AdministratorAccess);
    expect(setup.data).toEqual({ setup: true });
    expect(login.data).toBeUndefined();
    expect(setup.canActivate).toBeUndefined();
    expect(login.canActivate).toBeUndefined();
  });

  it('keeps session guards on the layout and administrator guards on provider settings', async () => {
    const layout = routes.find((route) => route.path === '')!;
    const security = layout.children!.find((route) => route.path === 'account/security')!;
    const settings = layout.children!.find((route) => route.path === 'admin/authentication')!;

    expect(layout.canActivate).toEqual([authGuard]);
    expect(layout.canActivateChild).toEqual([authGuard]);
    expect(await security.loadComponent!()).toBe(AccountSecurity);
    expect(security.canActivate).toBeUndefined();
    expect(await settings.loadComponent!()).toBe(AuthenticationSettings);
    expect(settings.canActivate).toEqual([administratorGuard]);
  });
});
