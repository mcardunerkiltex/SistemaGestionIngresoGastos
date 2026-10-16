import { HttpErrorResponse } from '@angular/common/http';

/** Convierte un error de la API (ProblemDetails) en un mensaje para mostrar. */
export function problemMessage(error: unknown, fallback = 'Algo salió mal. Probá de nuevo.'): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  if (error.status === 0) return 'No hay conexión con el servidor.';
  if (error.status === 429) return 'Demasiados intentos. Esperá un minuto y probá de nuevo.';
  const body = error.error as { detail?: string; errors?: Record<string, string[]> } | null;
  const firstFieldError = body?.errors ? Object.values(body.errors).flat()[0] : undefined;
  return body?.detail ?? firstFieldError ?? fallback;
}
