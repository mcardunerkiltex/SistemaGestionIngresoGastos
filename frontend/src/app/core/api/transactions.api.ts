import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type TransactionKind = 'Expense' | 'Income' | 'Transfer';

export interface Transaction {
  id: string;
  kind: TransactionKind;
  amount: number;
  /** Fecha sin hora, "YYYY-MM-DD". */
  date: string;
  note: string | null;
  account: { id: string; name: string };
  category: { id: string; name: string; parentName: string | null; icon: string | null; color: string | null } | null;
  toAccount: { id: string; name: string } | null;
}

export interface TransactionFilter {
  from?: string;
  to?: string;
  accountId?: string;
  categoryId?: string;
  kind?: TransactionKind;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface TransactionPage {
  items: Transaction[];
  total: number;
  page: number;
  pageSize: number;
  totalIncome: number;
  totalExpense: number;
}

export interface SaveTransaction {
  kind: 'Expense' | 'Income';
  accountId: string;
  categoryId: string;
  amount: number;
  date: string;
  note: string | null;
}

export interface SaveTransfer {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  date: string;
  note: string | null;
}

@Injectable({ providedIn: 'root' })
export class TransactionsApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/v1/transactions';

  list(filter: TransactionFilter): Promise<TransactionPage> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '') params = params.set(key, String(value));
    }
    return firstValueFrom(this.http.get<TransactionPage>(this.url, { params }));
  }

  get(id: string): Promise<Transaction> {
    return firstValueFrom(this.http.get<Transaction>(`${this.url}/${id}`));
  }

  create(body: SaveTransaction): Promise<Transaction> {
    return firstValueFrom(this.http.post<Transaction>(this.url, body));
  }

  update(id: string, body: SaveTransaction): Promise<Transaction> {
    return firstValueFrom(this.http.put<Transaction>(`${this.url}/${id}`, body));
  }

  createTransfer(body: SaveTransfer): Promise<Transaction> {
    return firstValueFrom(this.http.post<Transaction>(`${this.url}/transfers`, body));
  }

  updateTransfer(id: string, body: SaveTransfer): Promise<Transaction> {
    return firstValueFrom(this.http.put<Transaction>(`${this.url}/transfers/${id}`, body));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.url}/${id}`));
  }
}
