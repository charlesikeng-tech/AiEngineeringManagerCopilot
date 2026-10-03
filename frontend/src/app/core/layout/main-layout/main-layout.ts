import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Auth } from '@core/auth/auth';
import { TeamContext } from '@core/team/team-context';

import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageSelector } from '@core/i18n/language-selector/language-selector';
import { TeamSelector } from '@core/team/team-selector/team-selector';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NzButtonModule,
    NzIconModule,
    NzLayoutModule,
    NzMenuModule,
    TeamSelector,
    TranslatePipe,
    LanguageSelector,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayout {
  private readonly auth = inject(Auth);
  private readonly teamContext = inject(TeamContext);
  private readonly router = inject(Router);
  readonly isCollapsed = signal(false);

  logout(): void {
    this.auth.logout().subscribe({
      next: () => {
        this.teamContext.clearTeam();
        void this.router.navigateByUrl('/admin/login');
      },
    });
  }

  toggleSidebar(): void {
    this.isCollapsed.update((value) => !value);
  }
}
