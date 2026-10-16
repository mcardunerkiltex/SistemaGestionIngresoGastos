import { Routes } from '@angular/router';
import { Shell } from './layout/shell';

// Cada sección se carga bajo demanda: sumar un módulo nuevo no agranda la carga inicial.
export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      { path: 'inicio', title: 'Inicio · Mis Gastos', loadComponent: () => import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage) },
      { path: 'movimientos', title: 'Movimientos · Mis Gastos', loadComponent: () => import('./features/transactions/transactions.page').then((m) => m.TransactionsPage) },
      { path: 'movimientos/nuevo', title: 'Cargar · Mis Gastos', loadComponent: () => import('./features/transactions/new-transaction.page').then((m) => m.NewTransactionPage) },
      { path: 'presupuesto', title: 'Presupuesto · Mis Gastos', loadComponent: () => import('./features/budget/budget.page').then((m) => m.BudgetPage) },
      { path: 'configuracion', title: 'Configuración · Mis Gastos', loadComponent: () => import('./features/settings/settings.page').then((m) => m.SettingsPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
