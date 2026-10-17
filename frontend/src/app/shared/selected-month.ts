import { Injectable, computed, signal } from '@angular/core';
import { addMonths, toIsoDate } from './dates';

/** Mes elegido, compartido entre inicio, movimientos y presupuesto para no perderlo al navegar. */
@Injectable({ providedIn: 'root' })
export class SelectedMonth {
  readonly month = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  /** El mes como "YYYY-MM-01". */
  readonly iso = computed(() => toIsoDate(this.month()));

  move(delta: number): void {
    this.month.set(addMonths(this.month(), delta));
  }
}
