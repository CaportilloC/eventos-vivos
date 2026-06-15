import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { NotificationService } from '../../../../core/services/notification.service';
import { EventsApiService } from '../../../../core/api/events-api.service';
import { EventResponse } from '../../../../core/models/event.model';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [
    DatePipe,
    CurrencyPipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    StatusChipComponent,
    LoadingStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <div class="page-container">
      <!-- Page header -->
      <div class="page-header">
        <div class="header-left">
          <button mat-icon-button routerLink="/events">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <div>
            <h1>Detalle del Evento</h1>
            <p class="page-subtitle">Información completa del evento seleccionado</p>
          </div>
        </div>
      </div>

      @if (loading()) {
        <app-loading-state message="Cargando evento..." />
      }

      @if (error() && !loading()) {
        <app-error-state
          title="Error al cargar evento"
          [message]="error()"
          (retry)="loadEvent()"
        />
      }

      @if (event(); as evt) {
        <mat-card class="detail-card" appearance="outlined">
          <!-- Card header with status -->
          <mat-card-header>
            <mat-card-title>{{ evt.title }}</mat-card-title>
            <mat-card-subtitle>
              <app-status-chip [status]="evt.status" />
            </mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            @if (evt.description) {
              <p class="description">{{ evt.description }}</p>
              <mat-divider class="section-divider" />
            }

            <div class="info-grid">
              <div class="info-item">
                <span class="info-label">Tipo</span>
                <span class="info-value">{{ evt.type }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Lugar</span>
                <span class="info-value">ID #{{ evt.venueId }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Inicio</span>
                <span class="info-value">{{ evt.startsAt | date:'dd/MM/yyyy HH:mm' }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Fin</span>
                <span class="info-value">{{ evt.endsAt | date:'dd/MM/yyyy HH:mm' }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Capacidad</span>
                <span class="info-value">{{ evt.maxCapacity }} personas</span>
              </div>
              <div class="info-item">
                <span class="info-label">Precio</span>
                <span class="info-value price">{{ evt.price | currency:'USD':'symbol':'1.2-2' }}</span>
              </div>
            </div>
          </mat-card-content>

          <!-- Actions -->
          <mat-card-actions class="detail-actions">
            @if (evt.status === 'activo') {
              <button
                mat-raised-button
                color="primary"
                [routerLink]="['/reservations/create']"
                [queryParams]="{ eventId: evt.id }"
              >
                <mat-icon>confirmation_number</mat-icon>
                Reservar
              </button>
              <button
                mat-stroked-button
                color="primary"
                [routerLink]="['/events', evt.id, 'edit']"
              >
                <mat-icon>edit</mat-icon>
                Editar
              </button>
              <button
                mat-stroked-button
                color="warn"
                (click)="cancelEvent()"
                [disabled]="canceling()"
              >
                <mat-icon>cancel</mat-icon>
                Cancelar Evento
              </button>
            }
            <button
              mat-stroked-button
              [routerLink]="['/reports', evt.id]"
            >
              <mat-icon>bar_chart</mat-icon>
              Ver Reporte
            </button>
            <button
              mat-stroked-button
              [routerLink]="['/reservations']"
              [queryParams]="{ eventId: evt.id }"
            >
              <mat-icon>list_alt</mat-icon>
              Ver Reservas
            </button>
          </mat-card-actions>
        </mat-card>
      }
    </div>
  `,
  styles: `
    .page-subtitle {
      margin: 4px 0 0;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
    }

    .detail-card {
      max-width: 800px;
      margin: 0 auto 24px;
      border-radius: var(--ev-radius-md);
    }

    .description {
      margin: 16px 0;
      color: var(--mat-sys-on-surface-variant);
      line-height: 1.6;
    }

    .section-divider {
      margin: 16px 0;
    }

    .info-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
      margin-top: 8px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
      padding: 12px;
      background: var(--ev-surface-page);
      border-radius: var(--ev-radius-sm);
    }

    .info-label {
      font-size: 11px;
      font-weight: 600;
      color: var(--mat-sys-on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.06em;
    }

    .info-value {
      font-size: 15px;
      color: var(--mat-sys-on-surface);
      font-weight: 500;
    }

    .info-value.price {
      color: var(--mat-sys-primary);
      font-weight: 600;
    }

    .detail-actions {
      display: flex;
      gap: 12px;
      padding: 16px;
      flex-wrap: wrap;
    }

    .detail-actions button {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    @media (max-width: 600px) {
      .info-grid {
        grid-template-columns: 1fr;
      }
      .detail-actions {
        flex-direction: column;
      }
    }
  `,
})
export class EventDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly eventsApi = inject(EventsApiService);
  private readonly notification = inject(NotificationService);

  protected readonly event = signal<EventResponse | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly canceling = signal(false);

  private eventId = '';

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.eventId = id;
        this.loadEvent();
      }
    });
  }

  protected loadEvent(): void {
    this.loading.set(true);
    this.error.set(null);
    this.eventsApi.getById(this.eventId).subscribe({
      next: (evt) => {
        this.event.set(evt);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.detail || 'Error al cargar el evento');
        this.loading.set(false);
      },
    });
  }

  protected async cancelEvent(): Promise<void> {
    const result = await this.notification.confirmDanger(
      '¿Cancelar evento?',
      'Esta acción no se puede deshacer. Se cancelarán todas las reservas asociadas.',
    );
    if (!result.isConfirmed) return;

    this.canceling.set(true);
    this.eventsApi.cancel(this.eventId).subscribe({
      next: () => {
        this.canceling.set(false);
        this.notification.success('Evento cancelado', 'El evento ha sido cancelado exitosamente');
        this.router.navigate(['/events']);
      },
      error: () => {
        this.canceling.set(false);
        this.notification.error('Error', 'No se pudo cancelar el evento');
      },
    });
  }
}
