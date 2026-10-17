import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { Component, computed, inject, resource } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { BudgetsApi } from '../../core/api/budgets.api';
import { Transaction, TransactionsApi } from '../../core/api/transactions.api';
import { fromIsoDate, monthRange } from '../../shared/dates';
import { MonthNav } from '../../shared/month-nav';
import { budgetState, usedPercent } from '../../shared/progress';
import { SelectedMonth } from '../../shared/selected-month';

/** Inicio: cómo viene el mes (ingresos, gastos, presupuesto, en qué se fue la plata). */
@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, PercentPipe, RouterLink, MatButtonModule, MatIconModule, MonthNav],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage {
  private readonly budgets = inject(BudgetsApi);
  private readonly transactions = inject(TransactionsApi);
  private readonly selected = inject(SelectedMonth);

  protected readonly data = resource({
    params: () => ({ month: this.selected.iso() }),
    loader: ({ params }) => this.budgets.dashboard(params.month),
  });
  protected readonly recent = resource({
    params: () => monthRange(this.selected.month()),
    loader: ({ params }) => this.transactions.list({ ...params, pageSize: 5 }),
  });

  protected readonly saved = computed(() => {
    const d = this.data.value();
    return d ? d.income - d.expense : 0;
  });
  /** Escala de las barras de los últimos meses: el valor más alto. */
  private readonly historyMax = computed(() =>
    Math.max(1, ...(this.data.value()?.lastMonths ?? []).flatMap((m) => [m.income, m.expense])),
  );
  /** Escala de las barras por categoría: el mayor entre gastado y presupuestado. */
  private readonly categoryMax = computed(() =>
    Math.max(1, ...(this.data.value()?.byCategory ?? []).map((c) => Math.max(c.spent, c.budgeted))),
  );

  protected readonly usedPercent = usedPercent;
  protected readonly budgetState = budgetState;
  protected readonly toDate = fromIsoDate;

  protected historyHeight(value: number): number {
    return Math.round((value / this.historyMax()) * 100);
  }

  protected categoryWidth(value: number): number {
    return Math.round((value / this.categoryMax()) * 100);
  }

  protected signed(t: Transaction): number {
    return t.kind === 'Expense' ? -t.amount : t.amount;
  }
}
