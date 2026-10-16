import { Component, inject, input, resource, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { problemMessage } from '../../core/api/problem';
import { AuthService } from '../../core/auth/auth.service';
import { GoogleButton } from './google-button';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatProgressSpinnerModule, GoogleButton],
  template: `
    <mat-card appearance="outlined" class="card">
      <mat-card-content>
        <h1 class="brand">Mis Gastos</h1>
        <p class="subtitle">Ingresá para ver tus gastos.</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field>
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Contraseña</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="current-password" />
          </mat-form-field>
          @if (error()) {
            <p class="error" role="alert">{{ error() }}</p>
          }
          <button mat-flat-button class="submit" type="submit" [disabled]="busy()">
            @if (busy()) { <mat-spinner diameter="20" /> } @else { Ingresar }
          </button>
        </form>

        @if (googleClientId.value(); as clientId) {
          <div class="divider">o</div>
          <app-google-button [clientId]="clientId" (credential)="google($event)" />
        }

        <p class="switch">¿No tenés cuenta? <a routerLink="/registro" [queryParams]="{ volver: volver() }">Creá una</a></p>
      </mat-card-content>
    </mat-card>
  `,
  styleUrl: './auth-layout.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Ruta a la que volver después de ingresar (query param ?volver=). */
  readonly volver = input<string>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly googleClientId = resource({
    loader: async () => (await this.auth.config()).googleClientId,
  });

  protected submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return Promise.resolve();
    }
    const { email, password } = this.form.getRawValue();
    return this.run(() => this.auth.login(email, password));
  }

  protected google(idToken: string): Promise<void> {
    return this.run(() => this.auth.loginWithGoogle(idToken));
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    try {
      await action();
      await this.router.navigateByUrl(this.volver() || '/inicio');
    } catch (e) {
      this.error.set(problemMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
}
