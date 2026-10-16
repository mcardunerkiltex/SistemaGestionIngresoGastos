import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, linkedSignal, resource, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { AccountsApi } from '../../core/api/accounts.api';
import { CategoriesApi } from '../../core/api/categories.api';
import { Transaction, TransactionsApi } from '../../core/api/transactions.api';
import { addMonths, fromIsoDate, monthRange } from '../../shared/dates';

const PAGE_SIZE = 50;

interface DayGroup {
  date: string;
  items: Transaction[];
}

@Component({
  selector: 'app-transactions',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule, MatIconModule, MatFormFieldModule, MatSelectModule, MatInputModule],
  templateUrl: './transactions.page.html',
  styleUrl: './transactions.page.scss',
})
export class TransactionsPage {
  private readonly api = inject(TransactionsApi);

  protected readonly month = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  protected readonly accountId = signal<string>('');
  protected readonly categoryId = signal<string>('');
  protected readonly search = signal('');
  protected readonly pages = linkedSignal({
    source: () => [this.month(), this.accountId(), this.categoryId(), this.search()],
    computation: () => 1,
  });

  private readonly accountsApi = inject(AccountsApi);
  private readonly categoriesApi = inject(CategoriesApi);
  protected readonly accounts = resource({ loader: () => this.accountsApi.list(true) });
  protected readonly categories = resource({ loader: () => this.categoriesApi.list() });

  protected readonly result = resource({
    params: () => ({
      ...monthRange(this.month()),
      accountId: this.accountId() || undefined,
      categoryId: this.categoryId() || undefined,
      search: this.search().trim() || undefined,
      pageSize: PAGE_SIZE * this.pages(),
    }),
    loader: ({ params }) => this.api.list(params),
  });

  protected readonly groups = computed<DayGroup[]>(() => {
    const groups: DayGroup[] = [];
    for (const t of this.result.value()?.items ?? []) {
      const last = groups.at(-1);
      if (last?.date === t.date) last.items.push(t);
      else groups.push({ date: t.date, items: [t] });
    }
    return groups;
  });
  protected readonly hasMore = computed(() => {
    const r = this.result.value();
    return !!r && r.items.length < r.total;
  });
  protected readonly balance = computed(() => {
    const r = this.result.value();
    return r ? r.totalIncome - r.totalExpense : 0;
  });

  protected readonly toDate = fromIsoDate;

  protected moveMonth(delta: number): void {
    this.month.set(addMonths(this.month(), delta));
  }

  protected signedAmount(t: Transaction): number {
    if (t.kind === 'Expense') return -t.amount;
    if (t.kind === 'Income') return t.amount;
    // Transferencia: si se filtra por cuenta, se ve como salida o entrada de esa cuenta.
    const account = this.accountId();
    if (account && t.account.id === account) return -t.amount;
    return account ? t.amount : 0;
  }

  protected title(t: Transaction): string {
    if (t.kind === 'Transfer') return `${t.account.name} → ${t.toAccount?.name ?? ''}`;
    return t.category ? t.category.name : 'Sin categoría';
  }

  protected subtitle(t: Transaction): string {
    const parts = t.kind === 'Transfer' ? ['Transferencia'] : [t.category?.parentName, t.account.name];
    if (t.note) parts.push(t.note);
    return parts.filter(Boolean).join(' · ');
  }
}
