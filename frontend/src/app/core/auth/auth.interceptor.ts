import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const PUBLIC_AUTH = /\/api\/v1\/auth\/(config|login|register|google|refresh|logout)$/;

/** Agrega el access token a cada llamada a la API y, si venció, lo renueva una vez y reintenta. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/') || PUBLIC_AUTH.test(req.url)) return next(req);

  const auth = inject(AuthService);
  const withToken = (r: HttpRequest<unknown>) => {
    const token = auth.accessToken();
    return token ? r.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : r;
  };

  return next(withToken(req)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) return throwError(() => error);
      return from(auth.refresh()).pipe(
        switchMap((ok) => {
          if (ok) return next(withToken(req));
          void auth.expire();
          return throwError(() => error);
        }),
      );
    }),
  );
};
