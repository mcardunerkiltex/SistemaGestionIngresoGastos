import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthConfig, AuthResponse, User } from './auth.models';

const API = '/api/v1/auth';

/**
 * Sesión del usuario. El access token vive solo en memoria; el refresh token está en una
 * cookie httpOnly que maneja el navegador, así que un script malicioso no puede robarlo.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<AuthResponse | null>(null);
  private refreshing: Promise<boolean> | null = null;

  readonly user = computed<User | null>(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly accessToken = () => this.session()?.accessToken ?? null;

  config(): Promise<AuthConfig> {
    return firstValueFrom(this.http.get<AuthConfig>(`${API}/config`));
  }

  async login(email: string, password: string): Promise<void> {
    this.session.set(await firstValueFrom(this.http.post<AuthResponse>(`${API}/login`, { email, password })));
  }

  async register(displayName: string, email: string, password: string): Promise<void> {
    this.session.set(
      await firstValueFrom(this.http.post<AuthResponse>(`${API}/register`, { displayName, email, password })),
    );
  }

  async loginWithGoogle(idToken: string): Promise<void> {
    this.session.set(await firstValueFrom(this.http.post<AuthResponse>(`${API}/google`, { idToken })));
  }

  /** Pide un access token nuevo con la cookie. Las llamadas simultáneas comparten el mismo pedido. */
  refresh(): Promise<boolean> {
    this.refreshing ??= firstValueFrom(this.http.post<AuthResponse>(`${API}/refresh`, null))
      .then((response) => {
        this.session.set(response);
        return true;
      })
      .catch(() => {
        this.session.set(null);
        return false;
      })
      .finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`${API}/logout`, null));
    } finally {
      this.session.set(null);
      await this.router.navigateByUrl('/ingresar');
    }
  }

  /** Se llama una vez al abrir la app para recuperar la sesión si la cookie sigue vigente. */
  async restore(): Promise<void> {
    await this.refresh();
  }

  /** Se llama cuando la sesión venció y no se pudo renovar. */
  async expire(): Promise<void> {
    this.session.set(null);
    await this.router.navigate(['/ingresar'], { queryParams: { volver: this.router.url } });
  }
}
