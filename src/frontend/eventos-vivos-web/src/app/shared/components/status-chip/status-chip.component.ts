import { Component, computed, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { normalizeStatus } from '../../utils/normalize-status.util';

@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [NgClass],
  template: `
    <span class="status-chip" [ngClass]="chipClass()">
      <span class="status-dot"></span>
      {{ label() }}
    </span>
  `,
  styles: `
    .status-chip {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 12px 4px 10px;
      border-radius: 100px;
      font-size: 12px;
      font-weight: 600;
      line-height: 18px;
      letter-spacing: 0.01em;
      white-space: nowrap;
      transition: box-shadow var(--ev-transition-fast);
    }

    .status-dot {
      display: inline-block;
      width: 6px;
      height: 6px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    /* Activo — green */
    .status-activo {
      background-color: #e8f5e9;
      color: #1b5e20;
    }
    .status-activo .status-dot {
      background-color: #2e7d32;
    }

    /* Cancelado — red */
    .status-cancelado {
      background-color: #fbe9e7;
      color: #b71c1c;
    }
    .status-cancelado .status-dot {
      background-color: #c62828;
    }

    /* Completado — blue */
    .status-completado {
      background-color: #e3f2fd;
      color: #0d47a1;
    }
    .status-completado .status-dot {
      background-color: #1565c0;
    }

    /* Pendiente pago — amber */
    .status-pendiente_pago {
      background-color: #fff8e1;
      color: #e65100;
    }
    .status-pendiente_pago .status-dot {
      background-color: #f57f17;
    }

    /* Confirmada — green darker */
    .status-confirmada {
      background-color: #e8f5e9;
      color: #1b5e20;
    }
    .status-confirmada .status-dot {
      background-color: #2e7d32;
    }

    /* Perdida — purple */
    .status-perdida {
      background-color: #f3e5f5;
      color: #4a148c;
    }
    .status-perdida .status-dot {
      background-color: #6a1b9a;
    }

    /* Expirada — gray */
    .status-expirada {
      background-color: #eceff1;
      color: #37474f;
    }
    .status-expirada .status-dot {
      background-color: #607d8b;
    }
  `,
})
export class StatusChipComponent {
  readonly status = input.required<string>();

  /** Normalized key for CSS class and label lookup — lowercase_snake_case */
  readonly normalized = computed(() => normalizeStatus(this.status()));

  readonly label = computed(() => this.statusLabel(this.normalized()));
  readonly chipClass = computed(() => `status-${this.normalized()}`);

  private statusLabel(status: string): string {
    const labels: Record<string, string> = {
      activo: 'Activo',
      cancelado: 'Cancelado',
      completado: 'Completado',
      pendiente_pago: 'Pendiente',
      confirmada: 'Confirmada',
      perdida: 'Perdida',
      expirada: 'Expirada',
    };
    return labels[status] ?? status;
  }
}
