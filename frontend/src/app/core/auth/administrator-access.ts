import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { finalize, switchMap, tap } from 'rxjs';
import { AppInitializer } from '@core/app/app-initializer';
import { Auth } from './auth';

@Component({
  selector: 'app-administrator-access',
  imports: [FormsModule, NzButtonModule, NzInputModule, NzAlertModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="access">
      <h1>{{ setupMode ? 'Installation initiale — Administrateur' : 'Connexion Administrateur' }}</h1>
      <p>{{ setupMode
        ? "Installation unique réservée à l’opérateur de la plateforme. Le secret est fourni par l’exploitant."
        : "Compte administrateur local de récupération, indépendant du SSO." }}</p>
      @if (!setupMode) { <a routerLink="/login">Connexion SSO des utilisateurs</a> }
      @if (error()) { <nz-alert nzType="error" [nzMessage]="error()" nzShowIcon /> }
      @if (ready()) {
        <form (ngSubmit)="submit()">
          @if (setupMode) {
            <label for="secret">Secret d’installation</label>
            <input nz-input id="secret" name="secret" type="password" [(ngModel)]="secret" required maxlength="512" autocomplete="off" />
            <label for="name">Nom de l’administrateur</label>
            <input nz-input id="name" name="name" [(ngModel)]="name" required maxlength="200" autocomplete="name" />
          }
          <label for="email">Adresse e-mail</label>
          <input nz-input id="email" name="email" type="email" [(ngModel)]="email" required maxlength="254" autocomplete="username" />
          <label for="password">Mot de passe</label>
          <input nz-input id="password" name="password" type="password" [(ngModel)]="password" required maxlength="128"
            [attr.autocomplete]="setupMode ? 'new-password' : 'current-password'" />
          @if (setupMode) {
            <p>12 caractères minimum, majuscule, minuscule, chiffre et symbole.</p>
            <label for="confirmation">Confirmer le mot de passe</label>
            <input nz-input id="confirmation" name="confirmation" type="password" [(ngModel)]="confirmation" required maxlength="128" autocomplete="new-password" />
          }
          <button nz-button nzType="primary" type="submit" [nzLoading]="busy()" [disabled]="busy()">
            {{ setupMode ? 'Installer et se connecter' : 'Se connecter' }}
          </button>
        </form>
      }
      <p>Récupération : contactez l’exploitant. Supprimer des utilisateurs ne réactive pas l’installation.</p>
    </main>
  `,
  styles: [`
    .access { max-width: 480px; margin: 8vh auto; padding: 24px; }
    form { display: grid; gap: 12px; margin: 24px 0; }
    label { font-weight: 600; }
  `],
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

  ngOnInit() {
    if (this.auth.user()?.role === 'PlatformAdministrator') { void this.router.navigateByUrl('/dashboard'); return; }
    if (this.setupMode) {
      this.auth.setupStatus().subscribe({
        next: ({ setupAvailable }) => {
          if (setupAvailable) this.ready.set(true);
          else void this.router.navigateByUrl('/admin/login');
        },
        error: () => this.error.set('Impossible de vérifier l’installation. Rechargez la page.'),
      });
    }
  }

  submit() {
    if (this.busy()) return;
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
    request.pipe(
      tap(() => { authenticated = true; }),
      switchMap(() => this.initializer.initializeTeams()),
      finalize(() => { this.busy.set(false); this.secret = ''; this.password = ''; this.confirmation = ''; }),
    ).subscribe({
      next: () => void this.router.navigateByUrl('/dashboard'),
      error: (error: HttpErrorResponse) => {
        if (authenticated) {
          this.error.set('Authentification réussie, mais le chargement des équipes a échoué. Rechargez la page.');
          return;
        }
        if (error.status === 409) { void this.router.navigateByUrl('/admin/login'); return; }
        this.error.set(error.status === 429
          ? 'Trop de tentatives. Réessayez plus tard.'
          : this.setupMode ? 'Installation refusée. Vérifiez le secret, les informations et la politique de mot de passe.'
          : 'Connexion refusée. Vérifiez les identifiants ou réessayez dans 15 minutes.');
      },
    });
  }
}
