import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

/**
 * Estructura común de la app: menú lateral en escritorio y barra inferior con botón "+"
 * en el celular, para cargar un gasto con un toque.
 */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly isHandset = toSignal(
    inject(BreakpointObserver)
      .observe(Breakpoints.Handset)
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );

  protected readonly nav: NavItem[] = [
    { path: '/inicio', label: 'Inicio', icon: 'home' },
    { path: '/movimientos', label: 'Movimientos', icon: 'receipt_long' },
    { path: '/presupuesto', label: 'Presupuesto', icon: 'savings' },
    { path: '/configuracion', label: 'Más', icon: 'menu' },
  ];
}
