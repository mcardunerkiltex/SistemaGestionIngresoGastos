import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';

const session = {
  accessToken: 'token-1',
  expiresAt: '2030-01-01T00:00:00Z',
  user: { id: '1', email: 'm@test.com', displayName: 'Mariano', hasPassword: true, hasGoogle: false },
};

describe('App', () => {
  let http: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  async function signIn() {
    const login = auth.login('m@test.com', 'Clave1234');
    http.expectOne('/api/v1/auth/login').flush(session);
    await login;
  }

  it('sends visitors without a session to the login page', async () => {
    const harness = await RouterTestingHarness.create('/inicio');
    http.match('/api/v1/auth/config').forEach((r) => r.flush({ googleClientId: null }));

    expect(harness.routeNativeElement?.textContent).toContain('Ingresá para ver tus gastos');
  });

  it('shows the month summary and the navigation once signed in', async () => {
    await signIn();
    const harness = await RouterTestingHarness.create('/');
    http.match((r) => r.url === '/api/v1/dashboard').forEach((r) =>
      r.flush({ month: '2026-10-01', income: 500000, expense: 120000, budgeted: 0, totalBalance: 380000, byCategory: [], lastMonths: [] }),
    );
    http.match((r) => r.url === '/api/v1/transactions').forEach((r) =>
      r.flush({ items: [], total: 0, page: 1, pageSize: 5, totalIncome: 0, totalExpense: 0 }),
    );
    await harness.fixture.whenStable();

    const page = harness.fixture.nativeElement as HTMLElement;
    expect(harness.routeNativeElement?.textContent).toContain('En qué gastaste');
    expect(harness.routeNativeElement?.textContent).toContain('Te queda');
    expect(page.textContent).toContain('Movimientos');
  });

  it('adds the bearer token and renews it once when it expires', async () => {
    await signIn();
    const http$ = TestBed.inject(HttpTestingController);
    const done = new Promise((resolve) => TestBed.inject(HttpClient).get('/api/v1/auth/me').subscribe(resolve));

    const first = http$.expectOne('/api/v1/auth/me');
    expect(first.request.headers.get('Authorization')).toBe('Bearer token-1');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });

    http$.expectOne('/api/v1/auth/refresh').flush({ ...session, accessToken: 'token-2' });
    await new Promise((resolve) => setTimeout(resolve));
    const retry = http$.expectOne('/api/v1/auth/me');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer token-2');
    retry.flush(session.user);

    await done;
  });

  it('clears the session when the refresh cookie is no longer valid', async () => {
    const restored = auth.restore();
    http.expectOne('/api/v1/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await restored;

    expect(auth.isAuthenticated()).toBe(false);
  });
});
