import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';

import { finalize, switchMap, tap } from 'rxjs';

import { AppInitializer } from '@core/app/app-initializer';

import { Auth } from '@core/auth/auth';

@Component({
  selector: 'app-administrator-access',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    NzAlertModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzSpinModule,
  ],
  templateUrl: './administrator-access.html',
  styleUrl: './administrator-access.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdministratorAccess implements OnInit {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly initializer = inject(AppInitializer);

  readonly setupMode = inject(ActivatedRoute).snapshot.data['setup'] === true;

  readonly busy = signal(false);
  readonly ready = signal(!this.setupMode);
  readonly error = signal('');

  secret = '';
  email = '';
  name = '';
  password = '';
  confirmation = '';

  ngOnInit(): void {
    if (this.setupMode && this.auth.user()) {
      void this.router.navigateByUrl('/dashboard');
      return;
    }

    if (this.setupMode) {
      this.auth.setupStatus().subscribe({
        next: ({ setupAvailable }) => {
          if (setupAvailable) {
            this.ready.set(true);
          } else {
            void this.router.navigateByUrl('/admin/login');
          }
        },

        error: () => {
          this.error.set('Impossible de vérifier l’installation. Rechargez la page.');
        },
      });
    }
  }

  submit(): void {
    if (this.busy()) {
      return;
    }

    if (this.setupMode && this.password !== this.confirmation) {
      this.error.set('Les mots de passe ne correspondent pas.');
      return;
    }

    this.busy.set(true);
    this.error.set('');

    const request = this.setupMode
      ? this.auth.setup(this.secret, this.email, this.name, this.password)
      : this.auth.login(this.email, this.password);

    let authenticated = false;

    request
      .pipe(
        tap(() => {
          authenticated = true;
        }),

        switchMap(() => this.initializer.initializeTeams()),

        finalize(() => {
          this.busy.set(false);

          this.secret = '';
          this.password = '';
          this.confirmation = '';
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/dashboard');
        },

        error: (error: HttpErrorResponse) => {
          if (authenticated) {
            this.error.set(
              'Authentification réussie, mais le chargement des équipes a échoué. Rechargez la page.',
            );
            return;
          }

          if (error.status === 409) {
            void this.router.navigateByUrl('/admin/login');
            return;
          }

          this.error.set(
            error.status === 429
              ? 'Trop de tentatives. Réessayez plus tard.'
              : this.setupMode
                ? 'Installation refusée. Vérifiez le secret, les informations et la politique de mot de passe.'
                : 'Connexion refusée. Vérifiez les identifiants ou réessayez dans 15 minutes.',
          );
        },
      });
  }
}
