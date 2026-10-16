import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, resource, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { ACCOUNT_TYPES, Account, AccountsApi } from '../../core/api/accounts.api';
import { problemMessage } from '../../core/api/problem';
import { confirm } from '../../shared/confirm-dialog';
import { AccountDialog } from './account-dialog';

@Component({
  selector: 'app-accounts',
  imports: [CurrencyPipe, RouterLink, MatListModule, MatIconModule, MatButtonModule, MatMenuModule, MatSlideToggleModule],
  template: `
    <header class="page-header">
      <a mat-icon-button routerLink="/configuracion" aria-label="Volver"><mat-icon>arrow_back</mat-icon></a>
      <h1>Cuentas</h1>
      <button mat-flat-button (click)="edit(null)"><mat-icon>add</mat-icon> Nueva</button>
    </header>

    @if (accounts.value(); as list) {
      <p class="total">Saldo total <strong>{{ total() | currency: 'ARS' : 'symbol-narrow' }}</strong></p>
      <mat-list>
        @for (a of list; track a.id) {
          <mat-list-item [class.archived]="a.isArchived">
            <mat-icon matListItemIcon>{{ icon(a) }}</mat-icon>
            <span matListItemTitle>{{ a.name }}</span>
            <span matListItemLine>{{ typeLabel(a) }}{{ a.isArchived ? ' · archivada' : '' }}</span>
            <span matListItemMeta class="meta">
              <span [class.negative]="a.balance < 0">{{ a.balance | currency: 'ARS' : 'symbol-narrow' }}</span>
              <button mat-icon-button [matMenuTriggerFor]="menu" [attr.aria-label]="'Opciones de ' + a.name">
                <mat-icon>more_vert</mat-icon>
              </button>
            </span>
            <mat-menu #menu="matMenu">
              <button mat-menu-item (click)="edit(a)"><mat-icon>edit</mat-icon> Editar</button>
              <button mat-menu-item (click)="remove(a)"><mat-icon>delete</mat-icon> Borrar</button>
            </mat-menu>
          </mat-list-item>
        } @empty {
          <p>No tenés cuentas activas.</p>
        }
      </mat-list>
      <mat-slide-toggle [checked]="showArchived()" (change)="showArchived.set($event.checked)">Mostrar archivadas</mat-slide-toggle>
    } @else if (accounts.error()) {
      <p class="error">No se pudieron cargar las cuentas.</p>
    }
  `,
  styleUrl: './settings-pages.scss',
})
export class AccountsPage {
  private readonly api = inject(AccountsApi);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  protected readonly showArchived = signal(false);
  protected readonly accounts = resource({
    params: () => ({ archived: this.showArchived() }),
    loader: ({ params }) => this.api.list(params.archived),
  });
  protected readonly total = computed(() =>
    (this.accounts.value() ?? []).filter((a) => !a.isArchived).reduce((sum, a) => sum + a.balance, 0),
  );

  protected icon = (a: Account) => ACCOUNT_TYPES.find((t) => t.value === a.type)?.icon ?? 'wallet';
  protected typeLabel = (a: Account) => ACCOUNT_TYPES.find((t) => t.value === a.type)?.label ?? a.type;

  protected edit(account: Account | null): void {
    this.dialog
      .open(AccountDialog, { data: account })
      .afterClosed()
      .subscribe((saved) => saved && this.accounts.reload());
  }

  protected async remove(account: Account): Promise<void> {
    const ok = await confirm(this.dialog, {
      title: `¿Borrar "${account.name}"?`,
      message: 'Solo se pueden borrar cuentas sin movimientos. Si tiene, archivala desde Editar.',
      action: 'Borrar',
    });
    if (!ok) return;
    try {
      await this.api.delete(account.id);
      this.accounts.reload();
    } catch (e) {
      this.snack.open(problemMessage(e), 'OK', { duration: 6000 });
    }
  }
}
