import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { CategoriesApi, Category, CategoryType } from '../../core/api/categories.api';
import { problemMessage } from '../../core/api/problem';

export interface CategoryDialogData {
  category: Category | null;
  type: CategoryType;
  parent: Category | null;
}

const ICONS = ['home', 'bolt', 'restaurant', 'shopping_cart', 'directions_car', 'favorite', 'school', 'sports_esports', 'checkroom', 'receipt', 'pets', 'flight', 'fitness_center', 'child_care', 'card_giftcard', 'work', 'handyman', 'savings', 'add_circle', 'more_horiz'];
const COLORS = ['#5C6BC0', '#42A5F5', '#26C6DA', '#66BB6A', '#9CCC65', '#FFA726', '#EF5350', '#EC407A', '#AB47BC', '#8D6E63', '#78909C', '#2E7D32'];

@Component({
  selector: 'app-category-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ title }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="fields">
        <mat-form-field>
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" cdkFocusInitial />
        </mat-form-field>

        @if (!data.parent) {
          <span class="label">Ícono</span>
          <div class="swatches" role="radiogroup" aria-label="Ícono">
            @for (i of icons; track i) {
              <button type="button" class="swatch" role="radio" [attr.aria-checked]="form.value.icon === i" [class.selected]="form.value.icon === i" (click)="form.patchValue({ icon: i })">
                <mat-icon>{{ i }}</mat-icon>
              </button>
            }
          </div>
          <span class="label">Color</span>
          <div class="swatches" role="radiogroup" aria-label="Color">
            @for (c of colors; track c) {
              <button type="button" class="swatch color" role="radio" [attr.aria-label]="c" [attr.aria-checked]="form.value.color === c" [class.selected]="form.value.color === c" [style.background]="c" (click)="form.patchValue({ color: c })"></button>
            }
          </div>
        }
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancelar</button>
        <button mat-flat-button type="submit" [disabled]="busy()">Guardar</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .fields { display: flex; flex-direction: column; gap: 8px; min-width: min(360px, 80vw); }
    .label { font: var(--mat-sys-label-large); color: var(--mat-sys-on-surface-variant); }
    .swatches { display: flex; flex-wrap: wrap; gap: 8px; }
    .swatch {
      width: 40px; height: 40px; border-radius: 50%; display: grid; place-items: center; cursor: pointer;
      border: 2px solid transparent; background: var(--mat-sys-surface-container-high); color: var(--mat-sys-on-surface);
    }
    .swatch.selected { border-color: var(--mat-sys-primary); }
    .swatch.color.selected { outline: 2px solid var(--mat-sys-on-surface); outline-offset: 2px; }
    .error { color: var(--mat-sys-error); }
  `,
})
export class CategoryDialog {
  private readonly api = inject(CategoriesApi);
  private readonly ref = inject(MatDialogRef<CategoryDialog, Category>);
  protected readonly data = inject<CategoryDialogData>(MAT_DIALOG_DATA);
  protected readonly icons = ICONS;
  protected readonly colors = COLORS;
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  protected readonly title = this.data.category
    ? 'Editar categoría'
    : this.data.parent
      ? `Nueva subcategoría de ${this.data.parent.name}`
      : 'Nueva categoría';

  protected readonly form = inject(FormBuilder).group({
    name: [this.data.category?.name ?? '', [Validators.required, Validators.maxLength(80)]],
    icon: [this.data.category?.icon ?? this.data.parent?.icon ?? 'more_horiz'],
    color: [this.data.category?.color ?? this.data.parent?.color ?? COLORS[0]],
  });

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const { name, icon, color } = this.form.getRawValue();
    try {
      const saved = this.data.category
        ? await this.api.update(this.data.category.id, { name: name!, icon, color })
        : await this.api.create({ name: name!, icon, color, type: this.data.type, parentId: this.data.parent?.id ?? null });
      this.ref.close(saved);
    } catch (e) {
      this.error.set(problemMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
}
