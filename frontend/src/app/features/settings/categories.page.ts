import { Component, inject, resource, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { CategoriesApi, Category, CategoryType } from '../../core/api/categories.api';
import { problemMessage } from '../../core/api/problem';
import { confirm } from '../../shared/confirm-dialog';
import { CategoryDialog, CategoryDialogData } from './category-dialog';

@Component({
  selector: 'app-categories',
  imports: [RouterLink, MatButtonModule, MatButtonToggleModule, MatExpansionModule, MatIconModule, MatMenuModule],
  template: `
    <header class="page-header">
      <a mat-icon-button routerLink="/configuracion" aria-label="Volver"><mat-icon>arrow_back</mat-icon></a>
      <h1>Categorías</h1>
      <button mat-flat-button (click)="edit(null, null)"><mat-icon>add</mat-icon> Nueva</button>
    </header>

    <mat-button-toggle-group [value]="type()" (change)="type.set($event.value)" aria-label="Tipo de categoría" class="type-toggle">
      <mat-button-toggle value="Expense">Gastos</mat-button-toggle>
      <mat-button-toggle value="Income">Ingresos</mat-button-toggle>
    </mat-button-toggle-group>

    @if (categories.value(); as tree) {
      <mat-accordion multi>
        @for (c of tree; track c.id) {
          <mat-expansion-panel>
            <mat-expansion-panel-header>
              <mat-panel-title class="title">
                <span class="dot" [style.background]="c.color"><mat-icon>{{ c.icon }}</mat-icon></span>
                {{ c.name }}
              </mat-panel-title>
              <mat-panel-description class="count">{{ c.children.length || '' }}</mat-panel-description>
            </mat-expansion-panel-header>

            @for (s of c.children; track s.id) {
              <div class="sub">
                <span>{{ s.name }}</span>
                <button mat-icon-button [matMenuTriggerFor]="subMenu" [attr.aria-label]="'Opciones de ' + s.name"><mat-icon>more_vert</mat-icon></button>
                <mat-menu #subMenu="matMenu">
                  <button mat-menu-item (click)="edit(s, c)"><mat-icon>edit</mat-icon> Renombrar</button>
                  <button mat-menu-item (click)="remove(s)"><mat-icon>delete</mat-icon> Borrar</button>
                </mat-menu>
              </div>
            }
            <div class="actions">
              <button mat-button (click)="edit(null, c)"><mat-icon>add</mat-icon> Subcategoría</button>
              <button mat-button (click)="edit(c, null)"><mat-icon>edit</mat-icon> Editar</button>
              <button mat-button (click)="remove(c)"><mat-icon>delete</mat-icon> Borrar</button>
            </div>
          </mat-expansion-panel>
        }
      </mat-accordion>
    } @else if (categories.error()) {
      <p class="error">No se pudieron cargar las categorías.</p>
    }
  `,
  styleUrl: './settings-pages.scss',
})
export class CategoriesPage {
  private readonly api = inject(CategoriesApi);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  protected readonly type = signal<CategoryType>('Expense');
  protected readonly categories = resource({
    params: () => ({ type: this.type() }),
    loader: ({ params }) => this.api.list(params.type),
  });

  protected edit(category: Category | null, parent: Category | null): void {
    const data: CategoryDialogData = { category, parent, type: this.type() };
    this.dialog
      .open(CategoryDialog, { data })
      .afterClosed()
      .subscribe((saved) => saved && this.categories.reload());
  }

  protected async remove(category: Category): Promise<void> {
    const ok = await confirm(this.dialog, {
      title: `¿Borrar "${category.name}"?`,
      message: 'Solo se pueden borrar categorías sin subcategorías ni movimientos. Su presupuesto también se borra.',
      action: 'Borrar',
    });
    if (!ok) return;
    try {
      await this.api.delete(category.id);
      this.categories.reload();
    } catch (e) {
      this.snack.open(problemMessage(e), 'OK', { duration: 6000 });
    }
  }
}
