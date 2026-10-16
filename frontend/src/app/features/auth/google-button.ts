import { Component, ElementRef, inject, input, output, viewChild, afterNextRender } from '@angular/core';

interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleAccounts {
  id: {
    initialize(config: { client_id: string; callback: (r: GoogleCredentialResponse) => void }): void;
    renderButton(el: HTMLElement, options: Record<string, unknown>): void;
  };
}

declare global {
  interface Window {
    google?: { accounts: GoogleAccounts };
  }
}

let scriptLoading: Promise<void> | null = null;

function loadGoogleScript(): Promise<void> {
  scriptLoading ??= new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('No se pudo cargar Google'));
    document.head.appendChild(script);
  });
  return scriptLoading;
}

/** Botón oficial "Continuar con Google" (Google Identity Services). Emite el ID token. */
@Component({
  selector: 'app-google-button',
  template: '<div #host class="google-button"></div>',
  styles: ':host { display: flex; justify-content: center; min-height: 44px; }',
})
export class GoogleButton {
  readonly clientId = input.required<string>();
  readonly credential = output<string>();

  private readonly host = viewChild.required<ElementRef<HTMLElement>>('host');
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  constructor() {
    afterNextRender(async () => {
      await loadGoogleScript();
      const google = window.google!;
      google.accounts.id.initialize({
        client_id: this.clientId(),
        callback: (r) => this.credential.emit(r.credential),
      });
      google.accounts.id.renderButton(this.host().nativeElement, {
        theme: 'outline',
        size: 'large',
        text: 'continue_with',
        shape: 'pill',
        locale: 'es',
        width: Math.min(this.elementRef.nativeElement.clientWidth || 320, 400),
      });
    });
  }
}
