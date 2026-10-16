/** Porcentaje usado de un presupuesto, para las barras (tope 100). */
export function usedPercent(spent: number, budgeted: number): number {
  if (budgeted <= 0) return spent > 0 ? 100 : 0;
  return Math.min(100, Math.round((spent / budgeted) * 100));
}

/** Estado de un presupuesto: ok, cerca del límite (≥ 80 %) o pasado. */
export function budgetState(spent: number, budgeted: number): 'ok' | 'warn' | 'over' {
  if (budgeted <= 0) return 'ok';
  if (spent > budgeted) return 'over';
  return spent >= budgeted * 0.8 ? 'warn' : 'ok';
}
