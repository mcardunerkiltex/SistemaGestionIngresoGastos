import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ApiInfoService } from '../../core/api/api-info.service';

@Component({
  selector: 'app-dashboard',
  imports: [MatCardModule, MatIconModule],
  template: `
    <h1>Inicio</h1>
    <p>Acá va el resumen del mes: ingresos vs gastos, gasto por categoría y avance del presupuesto.</p>

    <mat-card appearance="outlined">
      <mat-card-content class="status">
        @let status = api.info();
        @if (!status) {
          <span>Conectando con la API…</span>
        } @else if (status.ok) {
          <mat-icon class="ok">check_circle</mat-icon>
          <span>Conectado a la API ({{ status.info.environment }})</span>
        } @else {
          <mat-icon class="error">error</mat-icon>
          <span>No se pudo conectar con la API</span>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    .status { display: flex; align-items: center; gap: 8px; }
    .ok { color: var(--mat-sys-primary); }
    .error { color: var(--mat-sys-error); }
  `,
})
export class DashboardPage {
  protected readonly api = inject(ApiInfoService);
}
