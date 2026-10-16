import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ACCOUNT_TYPES, Account, AccountType, AccountsApi } from '../../core/api/accounts.api';
import { problemMessage } from '../../core/api/problem';

/** Alta y edición de una cuenta. Cierra con la cuenta guardada. */
@Component({
  selector: 'app-account-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatCheckboxModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ account ? 'Editar cuenta' : 'Nueva cuenta' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="fields">
        <mat-form-field>
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" placeholder="Ej: Banco Galicia" cdkFocusInitial />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Tipo</mat-label>
          <mat-select formControlName="type">
            @for (t of types; track t.value) {
              <mat-option [value]="t.value"><mat-icon>{{ t.icon }}</mat-icon> {{ t.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Saldo inicial</mat-label>
          <span matTextPrefix>$&nbsp;</span>
          <input matInput type="number" inputmode="decimal" step="0.01" formControlName="initialBalance" />
          <mat-hint>Lo que tenía al empezar a usar la app.</mat-hint>
        </mat-form-field>
        @if (account) {
          <mat-checkbox formControlName="isArchived">Archivada (no aparece al cargar movimientos)</mat-checkbox>
        }
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancelar</button>
        <button mat-flat-button type="submit" [disabled]="busy()">Guardar</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .fields { display: flex; flex-direction: column; gap: 4px; min-width: min(360px, 80vw); }
    .error { color: var(--mat-sys-error); }
  `,
})
export class AccountDialog {
  private readonly api = inject(AccountsApi);
  private readonly ref = inject(MatDialogRef<AccountDialog, Account>);
  protected readonly account = inject<Account | null>(MAT_DIALOG_DATA);
  protected readonly types = ACCOUNT_TYPES;
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: [this.account?.name ?? '', [Validators.required, Validators.maxLength(80)]],
    type: [this.account?.type ?? ('Bank' as AccountType), Validators.required],
    initialBalance: [this.account?.initialBalance ?? 0, Validators.required],
    isArchived: [this.account?.isArchived ?? false],
  });

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.error.set('');
    try {
      const body = this.form.getRawValue();
      const saved = this.account ? await this.api.update(this.account.id, body) : await this.api.create(body);
      this.ref.close(saved);
    } catch (e) {
      this.error.set(problemMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
}
