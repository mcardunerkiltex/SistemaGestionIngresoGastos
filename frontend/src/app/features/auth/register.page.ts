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
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatProgressSpinnerModule, GoogleButton],
  template: `
    <mat-card appearance="outlined" class="card">
      <mat-card-content>
        <h1 class="brand">Crear cuenta</h1>
        <p class="subtitle">Te armamos una cuenta "Efectivo" y categorías para arrancar.</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field>
            <mat-label>Nombre</mat-label>
            <input matInput formControlName="displayName" autocomplete="name" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            @if (form.controls.email.hasError('email')) {
              <mat-error>Ingresá un email válido.</mat-error>
            }
          </mat-form-field>
          <mat-form-field>
            <mat-label>Contraseña</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
            <mat-hint>Mínimo 8 caracteres, con letras y números.</mat-hint>
            @if (form.controls.password.invalid) {
              <mat-error>Mínimo 8 caracteres, con letras y números.</mat-error>
            }
          </mat-form-field>
          @if (error()) {
            <p class="error" role="alert">{{ error() }}</p>
          }
          <button mat-flat-button class="submit" type="submit" [disabled]="busy()">
            @if (busy()) { <mat-spinner diameter="20" /> } @else { Crear cuenta }
          </button>
        </form>

        @if (googleClientId.value(); as clientId) {
          <div class="divider">o</div>
          <app-google-button [clientId]="clientId" (credential)="google($event)" />
        }

        <p class="switch">¿Ya tenés cuenta? <a routerLink="/ingresar" [queryParams]="{ volver: volver() }">Ingresá</a></p>
      </mat-card-content>
    </mat-card>
  `,
  styleUrl: './auth-layout.scss',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly volver = input<string>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)]],
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
    const { displayName, email, password } = this.form.getRawValue();
    return this.run(() => this.auth.register(displayName, email, password));
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
