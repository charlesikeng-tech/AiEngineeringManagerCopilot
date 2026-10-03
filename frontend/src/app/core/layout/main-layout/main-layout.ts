import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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
  readonly isCollapsed = signal(false);

  toggleSidebar(): void {
    this.isCollapsed.update((value) => !value);
  }
}
