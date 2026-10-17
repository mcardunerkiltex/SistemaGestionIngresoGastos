import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, resource, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BudgetLine, BudgetsApi } from '../../core/api/budgets.api';
import { problemMessage } from '../../core/api/problem';
import { MonthNav } from '../../shared/month-nav';
import { budgetState, usedPercent } from '../../shared/progress';
import { SelectedMonth } from '../../shared/selected-month';
import { BudgetDialog } from './budget-dialog';

@Component({
  selector: 'app-budget',
  imports: [CurrencyPipe, MatButtonModule, MatIconModule, MonthNav],
  templateUrl: './budget.page.html',
  styleUrl: './budget.page.scss',
})
export class BudgetPage {
  private readonly api = inject(BudgetsApi);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly month = inject(SelectedMonth).iso;

  protected readonly data = resource({ params: () => ({ month: this.month() }), loader: ({ params }) => this.api.month(params.month) });
  protected readonly busy = signal(false);

  protected readonly planned = computed(() => this.data.value()?.lines.filter((l) => l.budgeted > 0) ?? []);
  protected readonly unplanned = computed(() => this.data.value()?.lines.filter((l) => l.budgeted === 0) ?? []);
  protected readonly available = computed(() => {
    const d = this.data.value();
    return d ? d.totalBudgeted - d.totalSpent : 0;
  });

  protected readonly usedPercent = usedPercent;
  protected readonly budgetState = budgetState;

  protected async edit(line: BudgetLine): Promise<void> {
    const ref = this.dialog.open(BudgetDialog, { data: line });
    const amount = await new Promise<number | undefined>((resolve) => ref.afterClosed().subscribe(resolve));
    if (amount === undefined || amount === line.budgeted) return;
    try {
      await this.api.set(this.month(), line.categoryId, amount);
      this.data.reload();
    } catch (e) {
      this.snack.open(problemMessage(e), 'OK');
    }
  }

  protected async copyPrevious(): Promise<void> {
    this.busy.set(true);
    try {
      this.data.set(await this.api.copyPrevious(this.month()));
      this.snack.open('Presupuesto copiado del mes anterior', undefined, { duration: 2500 });
    } catch (e) {
      this.snack.open(problemMessage(e), 'OK');
    } finally {
      this.busy.set(false);
    }
  }
}
