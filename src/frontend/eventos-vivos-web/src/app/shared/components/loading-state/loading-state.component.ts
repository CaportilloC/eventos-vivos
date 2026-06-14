import { Component, input } from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  imports: [MatProgressSpinner],
  template: `
    <div class="loading-container">
      <mat-spinner [diameter]="diameter()" class="loading-spinner" />
      @if (message()) {
        <div class="loading-content">
          <p class="loading-message">{{ message() }}</p>
          <p class="loading-hint">Espere un momento por favor</p>
        </div>
      }
    </div>
  `,
  styles: `
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 64px 48px;
      gap: 20px;
    }

    .loading-spinner {
      opacity: 0.8;
    }

    .loading-content {
      text-align: center;
    }

    .loading-message {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-large);
      font-weight: 500;
    }

    .loading-hint {
      margin: 4px 0 0;
      color: var(--mat-sys-outline);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class LoadingStateComponent {
  readonly diameter = input(48);
  readonly message = input('Cargando...');
}
