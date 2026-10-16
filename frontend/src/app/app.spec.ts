import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('redirects to Inicio and shows the navigation', async () => {
    const harness = await RouterTestingHarness.create('/');
    const el = harness.routeNativeElement as HTMLElement;
    const page = harness.fixture.nativeElement as HTMLElement;

    expect(el.querySelector('h1')?.textContent).toContain('Inicio');
    expect(page.textContent).toContain('Movimientos');
    expect(page.textContent).toContain('Presupuesto');
  });
});
