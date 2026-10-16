import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type AccountType = 'Cash' | 'Bank' | 'CreditCard' | 'Wallet';

export const ACCOUNT_TYPES: { value: AccountType; label: string; icon: string }[] = [
  { value: 'Cash', label: 'Efectivo', icon: 'payments' },
  { value: 'Bank', label: 'Cuenta bancaria', icon: 'account_balance' },
  { value: 'CreditCard', label: 'Tarjeta de crédito', icon: 'credit_card' },
  { value: 'Wallet', label: 'Billetera virtual', icon: 'account_balance_wallet' },
];

export interface Account {
  id: string;
  name: string;
  type: AccountType;
  currency: string;
  initialBalance: number;
  balance: number;
  isArchived: boolean;
}

export interface SaveAccount {
  name: string;
  type: AccountType;
  initialBalance: number;
  isArchived: boolean;
}

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/v1/accounts';

  list(includeArchived = false): Promise<Account[]> {
    return firstValueFrom(this.http.get<Account[]>(this.url, { params: { includeArchived } }));
  }

  create(body: SaveAccount): Promise<Account> {
    return firstValueFrom(this.http.post<Account>(this.url, body));
  }

  update(id: string, body: SaveAccount): Promise<Account> {
    return firstValueFrom(this.http.put<Account>(`${this.url}/${id}`, body));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.url}/${id}`));
  }
}
