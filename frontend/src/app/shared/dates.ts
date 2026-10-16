/** Fechas sin hora ("YYYY-MM-DD") en hora local, para no correr un día por la zona horaria. */
export function toIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function fromIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function today(): string {
  return toIsoDate(new Date());
}

/** Primer y último día del mes, como "YYYY-MM-DD". */
export function monthRange(month: Date): { from: string; to: string } {
  return {
    from: toIsoDate(new Date(month.getFullYear(), month.getMonth(), 1)),
    to: toIsoDate(new Date(month.getFullYear(), month.getMonth() + 1, 0)),
  };
}

export function addMonths(month: Date, delta: number): Date {
  return new Date(month.getFullYear(), month.getMonth() + delta, 1);
}
