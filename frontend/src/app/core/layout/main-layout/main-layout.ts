import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { Auth } from '@core/auth/auth';
import { LanguageSelector } from '@shared/ui/language-selector/language-selector';
import { TeamSelector } from '@shared/ui/team-selector/team-selector';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';

import { environment } from '@environments/environment';
import { AccountMenu } from '../account-menu/account-menu';

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
    LanguageSelector,
    TranslatePipe,
    AccountMenu,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayout {
  private readonly auth = inject(Auth);

  readonly isCollapsed = signal(false);

  readonly administrator = this.auth.user;

  readonly isDevelopment = !environment.production;

  toggleSidebar(): void {
    this.isCollapsed.update((value) => !value);
  }
}
