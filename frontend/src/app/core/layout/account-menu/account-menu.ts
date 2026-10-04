import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { Auth } from '@core/auth/auth';
import { TeamContext } from '@core/team/team-context';

import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDropdownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';

@Component({
  selector: 'app-account-menu',
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    NzAvatarModule,
    NzButtonModule,
    NzDropdownModule,
    NzIconModule,
  ],
  templateUrl: './account-menu.html',
  styleUrl: './account-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountMenu {
  private readonly auth = inject(Auth);
  private readonly teamContext = inject(TeamContext);
  private readonly router = inject(Router);

  readonly user = this.auth.user;

  readonly roleLabel = computed(() =>
    this.user()?.role === 'PlatformAdministrator' ? 'Administrator' : 'User',
  );

  logout(): void {
    this.auth.logout().subscribe({
      next: () => {
        this.teamContext.clearTeam();

        void this.router.navigateByUrl('/login');
      },
    });
  }
}
