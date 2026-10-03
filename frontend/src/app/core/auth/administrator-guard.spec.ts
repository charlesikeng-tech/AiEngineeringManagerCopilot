import { TestBed } from '@angular/core/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { administratorGuard } from './administrator-guard';
import { Auth } from './auth';

describe('administrator guard', () => {
  let role: string | undefined;
  beforeEach(() => {
    role = undefined;
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Auth, useValue: { user: () => role ? { role } : null } }],
    });
  });
  it('allows an explicit platform administrator', () => {
    role = 'PlatformAdministrator';
    expect(TestBed.runInInjectionContext(() => administratorGuard({} as never, {} as never))).toBe(true);
  });
  it('rejects an unauthenticated user or an ordinary authenticated role', () => {
    expect(TestBed.runInInjectionContext(() => administratorGuard({} as never, {} as never))).toBeInstanceOf(UrlTree);
    role = 'Member';
    expect(TestBed.runInInjectionContext(() => administratorGuard({} as never, {} as never))).toBeInstanceOf(UrlTree);
  });
});
