import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SelectedMonth } from './selected-month';

@Component({
  selector: 'app-month-nav',
  imports: [DatePipe, MatButtonModule, MatIconModule],
  template: `
    <button mat-icon-button (click)="selected.move(-1)" aria-label="Mes anterior"><mat-icon>chevron_left</mat-icon></button>
    <h1>{{ selected.month() | date: 'MMMM y' }}</h1>
    <button mat-icon-button (click)="selected.move(1)" aria-label="Mes siguiente"><mat-icon>chevron_right</mat-icon></button>
  `,
  styles: `
    :host { display: flex; align-items: center; justify-content: space-between; }
    h1 { margin: 0; font: var(--mat-sys-title-large); text-transform: capitalize; }
  `,
})
export class MonthNav {
  protected readonly selected = inject(SelectedMonth);
}
