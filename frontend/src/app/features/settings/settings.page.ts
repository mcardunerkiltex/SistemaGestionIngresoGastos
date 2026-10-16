import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-settings',
  imports: [MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <h1>Configuración</h1>

    @if (auth.user(); as user) {
      <mat-card appearance="outlined">
        <mat-card-header>
          <mat-card-title>{{ user.displayName }}</mat-card-title>
          <mat-card-subtitle>{{ user.email }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>
            Ingresás con
            {{ user.hasPassword && user.hasGoogle ? 'email y Google' : user.hasGoogle ? 'Google' : 'email y contraseña' }}.
          </p>
        </mat-card-content>
        <mat-card-actions>
          <button mat-button (click)="auth.logout()"><mat-icon>logout</mat-icon> Cerrar sesión</button>
        </mat-card-actions>
      </mat-card>
    }

    <p class="next">Cuentas y categorías se editan acá; se construyen en el siguiente bloque del paso 3.</p>
  `,
  styles: '.next { color: var(--mat-sys-on-surface-variant); margin-top: 24px; }',
})
export class SettingsPage {
  protected readonly auth = inject(AuthService);
}
