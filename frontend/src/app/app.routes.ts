import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';

// Cada sección se carga bajo demanda: sumar un módulo nuevo no agranda la carga inicial.
export const routes: Routes = [
  { path: 'ingresar', title: 'Ingresar · Mis Gastos', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login.page').then((m) => m.LoginPage) },
  { path: 'registro', title: 'Crear cuenta · Mis Gastos', canActivate: [guestGuard], loadComponent: () => import('./features/auth/register.page').then((m) => m.RegisterPage) },
  {
    path: '',
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      { path: 'inicio', title: 'Inicio · Mis Gastos', loadComponent: () => import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage) },
      { path: 'movimientos', title: 'Movimientos · Mis Gastos', loadComponent: () => import('./features/transactions/transactions.page').then((m) => m.TransactionsPage) },
      { path: 'movimientos/nuevo', title: 'Cargar · Mis Gastos', loadComponent: () => import('./features/transactions/transaction-form.page').then((m) => m.TransactionFormPage) },
      { path: 'movimientos/:id', title: 'Editar · Mis Gastos', loadComponent: () => import('./features/transactions/transaction-form.page').then((m) => m.TransactionFormPage) },
      { path: 'presupuesto', title: 'Presupuesto · Mis Gastos', loadComponent: () => import('./features/budget/budget.page').then((m) => m.BudgetPage) },
      { path: 'configuracion', title: 'Configuración · Mis Gastos', loadComponent: () => import('./features/settings/settings.page').then((m) => m.SettingsPage) },
      { path: 'configuracion/cuentas', title: 'Cuentas · Mis Gastos', loadComponent: () => import('./features/settings/accounts.page').then((m) => m.AccountsPage) },
      { path: 'configuracion/categorias', title: 'Categorías · Mis Gastos', loadComponent: () => import('./features/settings/categories.page').then((m) => m.CategoriesPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
