import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Solo usuarios con sesión. Si no, va a /ingresar y vuelve después. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() || inject(Router).createUrlTree(['/ingresar'], { queryParams: { volver: state.url } });
};

/** Login y registro solo para quien no tiene sesión. */
export const guestGuard: CanActivateFn = () =>
  !inject(AuthService).isAuthenticated() || inject(Router).createUrlTree(['/inicio']);
