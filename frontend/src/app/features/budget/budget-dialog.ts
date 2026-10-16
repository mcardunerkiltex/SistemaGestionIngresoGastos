import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { BudgetLine } from '../../core/api/budgets.api';

/** Pide el monto presupuestado para una categoría. Cierra con el monto (0 = sin presupuesto). */
@Component({
  selector: 'app-budget-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Presupuesto de {{ line.name }}</h2>
    <form (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field class="field">
          <mat-label>Monto para el mes</mat-label>
          <span matTextPrefix>$&nbsp;</span>
          <input matInput name="amount" type="number" inputmode="decimal" min="0" step="0.01"
                 [ngModel]="amount()" (ngModelChange)="amount.set($event)" cdkFocusInitial />
          <mat-hint>Dejalo vacío o en 0 para quitarlo.</mat-hint>
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancelar</button>
        <button mat-flat-button type="submit">Guardar</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `.field { width: min(320px, 75vw); }`,
})
export class BudgetDialog {
  private readonly ref = inject(MatDialogRef<BudgetDialog, number>);
  protected readonly line = inject<BudgetLine>(MAT_DIALOG_DATA);
  protected readonly amount = signal<number | null>(this.line.budgeted || null);

  protected save(): void {
    const value = Number(this.amount() ?? 0);
    this.ref.close(Number.isFinite(value) && value > 0 ? value : 0);
  }
}
