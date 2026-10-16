import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-settings',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatListModule],
  template: `
    <h1>Configuración</h1>

    <mat-nav-list>
      <a mat-list-item routerLink="cuentas">
        <mat-icon matListItemIcon>account_balance_wallet</mat-icon>
        <span matListItemTitle>Cuentas</span>
        <span matListItemLine>Efectivo, bancos, tarjetas y billeteras</span>
      </a>
      <a mat-list-item routerLink="categorias">
        <mat-icon matListItemIcon>category</mat-icon>
        <span matListItemTitle>Categorías</span>
        <span matListItemLine>De gastos e ingresos</span>
      </a>
    </mat-nav-list>

    @if (auth.user(); as user) {
      <mat-card appearance="outlined" class="profile">
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
  `,
  styles: '.profile { margin-top: 24px; }',
})
export class SettingsPage {
  protected readonly auth = inject(AuthService);
}
