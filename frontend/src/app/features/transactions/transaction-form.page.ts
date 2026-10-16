import { DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, input, resource, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { AccountsApi } from '../../core/api/accounts.api';
import { CategoriesApi, Category } from '../../core/api/categories.api';
import { problemMessage } from '../../core/api/problem';
import { TransactionKind, TransactionsApi } from '../../core/api/transactions.api';
import { confirm } from '../../shared/confirm-dialog';
import { fromIsoDate, toIsoDate, today } from '../../shared/dates';

const LAST_ACCOUNT_KEY = 'gastos.lastAccountId';

function readLastAccount(): string | null {
  try {
    return localStorage.getItem(LAST_ACCOUNT_KEY);
  } catch {
    return null;
  }
}

function rememberAccount(id: string): void {
  try {
    localStorage.setItem(LAST_ACCOUNT_KEY, id);
  } catch {
    // Sin almacenamiento local (modo privado): no pasa nada, solo no se recuerda la cuenta.
  }
}

/**
 * Carga rápida y edición de un movimiento. Pensada para el celular: el monto tiene el foco,
 * la cuenta y la fecha vienen completas, y la categoría se elige con un toque.
 */
@Component({
  selector: 'app-transaction-form',
  imports: [
    FormsModule,
    RouterLink,
    DecimalPipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './transaction-form.page.html',
  styleUrl: './transaction-form.page.scss',
})
export class TransactionFormPage {
  private readonly transactions = inject(TransactionsApi);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  /** Id del movimiento a editar (ruta /movimientos/:id). Sin id es un alta. */
  readonly id = input<string>();

  protected readonly kind = signal<TransactionKind>('Expense');
  protected readonly amount = signal<number | null>(null);
  protected readonly accountId = signal<string | null>(null);
  protected readonly toAccountId = signal<string | null>(null);
  protected readonly categoryId = signal<string | null>(null);
  protected readonly parentId = signal<string | null>(null);
  protected readonly date = signal<Date>(fromIsoDate(today()));
  protected readonly note = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  private readonly accountsApi = inject(AccountsApi);
  private readonly categoriesApi = inject(CategoriesApi);
  protected readonly accounts = resource({ loader: () => this.accountsApi.list() });
  protected readonly categories = resource({
    params: () => ({ kind: this.kind() }),
    loader: ({ params }) => (params.kind === 'Transfer' ? Promise.resolve([]) : this.categoriesApi.list(params.kind)),
  });
  private readonly existing = resource({
    params: () => ({ id: this.id() }),
    loader: ({ params }) => (params.id ? this.transactions.get(params.id) : Promise.resolve(null)),
  });

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly selectedParent = computed<Category | null>(
    () => this.categories.value()?.find((c) => c.id === this.parentId()) ?? null,
  );
  protected readonly selectedCategoryLabel = computed(() => {
    const parent = this.selectedParent();
    if (!parent) return '';
    const child = parent.children.find((c) => c.id === this.categoryId());
    return child ? `${parent.name} › ${child.name}` : parent.name;
  });

  constructor() {
    // Cuenta por defecto: la última usada, o la primera.
    effect(() => {
      const list = this.accounts.value();
      if (!list?.length || this.accountId() || this.isEdit()) return;
      const last = readLastAccount();
      this.accountId.set(list.find((a) => a.id === last)?.id ?? list[0].id);
    });

    // Al editar, carga el movimiento en el formulario.
    effect(() => {
      const t = this.existing.value();
      if (!t) return;
      this.kind.set(t.kind);
      this.amount.set(t.amount);
      this.accountId.set(t.account.id);
      this.toAccountId.set(t.toAccount?.id ?? null);
      this.date.set(fromIsoDate(t.date));
      this.note.set(t.note ?? '');
      this.categoryId.set(t.category?.id ?? null);
    });

    // Con las categorías cargadas, ubica la principal de la elegida.
    effect(() => {
      const tree = this.categories.value();
      const id = this.categoryId();
      if (!tree || !id || this.parentId()) return;
      const parent = tree.find((c) => c.id === id || c.children.some((s) => s.id === id));
      if (parent) this.parentId.set(parent.id);
    });
  }

  protected setKind(kind: TransactionKind): void {
    if (kind === this.kind()) return;
    this.kind.set(kind);
    this.categoryId.set(null);
    this.parentId.set(null);
  }

  protected pickParent(category: Category): void {
    this.parentId.set(category.id);
    this.categoryId.set(category.id);
  }

  protected pickChild(category: Category): void {
    this.categoryId.set(this.categoryId() === category.id ? this.parentId() : category.id);
  }

  protected async save(): Promise<void> {
    const amount = this.amount();
    const accountId = this.accountId();
    const kind = this.kind();
    const missing = !amount || amount <= 0 ? 'Ingresá un monto.' :
      !accountId ? 'Elegí una cuenta.' :
      kind === 'Transfer' && !this.toAccountId() ? 'Elegí la cuenta de destino.' :
      kind !== 'Transfer' && !this.categoryId() ? 'Elegí una categoría.' : '';
    if (missing) {
      this.error.set(missing);
      return;
    }

    this.busy.set(true);
    this.error.set('');
    const date = toIsoDate(this.date());
    const note = this.note().trim() || null;
    const id = this.id();
    try {
      if (kind === 'Transfer') {
        const body = { fromAccountId: accountId!, toAccountId: this.toAccountId()!, amount: amount!, date, note };
        await (id ? this.transactions.updateTransfer(id, body) : this.transactions.createTransfer(body));
      } else {
        const body = { kind, accountId: accountId!, categoryId: this.categoryId()!, amount: amount!, date, note };
        await (id ? this.transactions.update(id, body) : this.transactions.create(body));
      }
      rememberAccount(accountId!);
      this.snack.open(id ? 'Movimiento actualizado' : this.savedMessage(kind), undefined, { duration: 2500 });
      await this.router.navigateByUrl('/movimientos');
    } catch (e) {
      this.error.set(problemMessage(e));
    } finally {
      this.busy.set(false);
    }
  }

  protected async remove(): Promise<void> {
    const id = this.id();
    if (!id) return;
    const ok = await confirm(this.dialog, {
      title: '¿Borrar el movimiento?',
      message: this.kind() === 'Transfer' ? 'Se borran las dos partes de la transferencia.' : 'No se puede deshacer.',
      action: 'Borrar',
    });
    if (!ok) return;
    try {
      await this.transactions.delete(id);
      this.snack.open('Movimiento borrado', undefined, { duration: 2500 });
      await this.router.navigateByUrl('/movimientos');
    } catch (e) {
      this.error.set(problemMessage(e));
    }
  }

  private savedMessage(kind: TransactionKind): string {
    return kind === 'Expense' ? 'Gasto guardado' : kind === 'Income' ? 'Ingreso guardado' : 'Transferencia guardada';
  }
}
