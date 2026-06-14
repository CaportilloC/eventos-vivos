import { Component, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [MatIcon, MatButton],
  template: `
    <div class="error-container">
      <div class="error-icon-wrapper">
        <mat-icon class="error-icon">error_outline</mat-icon>
      </div>
      <h3 class="error-title">{{ title() }}</h3>
      @if (message()) {
        <p class="error-message">{{ message() }}</p>
      }
      @if (showRetry()) {
        <button mat-stroked-button color="primary" (click)="retry.emit()" class="error-retry-btn">
          <mat-icon>refresh</mat-icon>
          Intentar de nuevo
        </button>
      }
    </div>
  `,
  styles: `
    .error-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 64px 48px;
      gap: 12px;
      text-align: center;
    }

    .error-icon-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: #fbe9e7;
      margin-bottom: 4px;
    }

    .error-icon {
      font-size: 36px;
      width: 36px;
      height: 36px;
      color: var(--mat-sys-error);
    }

    .error-title {
      margin: 0;
      font: var(--mat-sys-title-medium);
      color: var(--mat-sys-on-surface);
    }

    .error-message {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
      max-width: 480px;
      line-height: 1.5;
    }

    .error-retry-btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      margin-top: 8px;
      border-radius: var(--ev-radius-sm);
    }
  `,
})
export class ErrorStateComponent {
  readonly title = input('Algo salió mal');
  readonly message = input<string | null>(null);
  readonly showRetry = input(true);
  readonly retry = output<void>();
}
