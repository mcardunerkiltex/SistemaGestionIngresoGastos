import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface BudgetLine {
  categoryId: string;
  name: string;
  icon: string | null;
  color: string | null;
  budgeted: number;
  spent: number;
}

export interface BudgetMonth {
  month: string;
  lines: BudgetLine[];
  totalBudgeted: number;
  totalSpent: number;
}

export interface CategorySpending {
  categoryId: string;
  name: string;
  icon: string | null;
  color: string | null;
  spent: number;
  budgeted: number;
}

export interface Dashboard {
  month: string;
  income: number;
  expense: number;
  budgeted: number;
  totalBalance: number;
  byCategory: CategorySpending[];
  lastMonths: { month: string; income: number; expense: number }[];
}

/** Presupuesto mensual y resumen del mes. Los meses van como "YYYY-MM-01". */
@Injectable({ providedIn: 'root' })
export class BudgetsApi {
  private readonly http = inject(HttpClient);

  month(month: string): Promise<BudgetMonth> {
    return firstValueFrom(this.http.get<BudgetMonth>(`/api/v1/budgets/${month}`));
  }

  set(month: string, categoryId: string, amount: number): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/v1/budgets/${month}/${categoryId}`, { amount }));
  }

  copyPrevious(month: string): Promise<BudgetMonth> {
    return firstValueFrom(this.http.post<BudgetMonth>(`/api/v1/budgets/${month}/copy-previous`, null));
  }

  dashboard(month: string): Promise<Dashboard> {
    return firstValueFrom(this.http.get<Dashboard>('/api/v1/dashboard', { params: { month } }));
  }
}
