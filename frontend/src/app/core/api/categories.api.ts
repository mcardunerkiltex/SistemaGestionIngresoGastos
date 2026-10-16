import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type CategoryType = 'Expense' | 'Income';

export interface Category {
  id: string;
  parentId: string | null;
  name: string;
  type: CategoryType;
  icon: string | null;
  color: string | null;
  children: Category[];
}

export interface CreateCategory {
  name: string;
  type: CategoryType;
  parentId: string | null;
  icon: string | null;
  color: string | null;
}

export type UpdateCategory = Pick<CreateCategory, 'name' | 'icon' | 'color'>;

@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/v1/categories';

  list(type?: CategoryType): Promise<Category[]> {
    return firstValueFrom(this.http.get<Category[]>(this.url, { params: type ? { type } : {} }));
  }

  create(body: CreateCategory): Promise<Category> {
    return firstValueFrom(this.http.post<Category>(this.url, body));
  }

  update(id: string, body: UpdateCategory): Promise<Category> {
    return firstValueFrom(this.http.put<Category>(`${this.url}/${id}`, body));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.url}/${id}`));
  }
}
