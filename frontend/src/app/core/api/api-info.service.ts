import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of } from 'rxjs';

export interface ApiInfo {
  name: string;
  version: string;
  environment: string;
}

/** Consulta /api/v1/info para mostrar si el backend está disponible. */
@Injectable({ providedIn: 'root' })
export class ApiInfoService {
  private readonly http = inject(HttpClient);

  readonly info = toSignal(
    this.http.get<ApiInfo>('/api/v1/info').pipe(
      map((info) => ({ ok: true as const, info })),
      catchError(() => of({ ok: false as const })),
    ),
  );
}
