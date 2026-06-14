import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { NotificationService } from '../../../../core/services/notification.service';
import { ReservationsApiService } from '../../../../core/api/reservations-api.service';
import { EventsApiService } from '../../../../core/api/events-api.service';
import { ReservationResponse } from '../../../../core/models/reservation.model';
import { EventResponse } from '../../../../core/models/event.model';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';

@Component({
  selector: 'app-reservation-detail',
  standalone: true,
  imports: [
    DatePipe,
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
          <button mat-icon-button routerLink="/reservations">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <div>
            <h1>Detalle de Reserva</h1>
            <p class="page-subtitle">Información completa de la reserva</p>
          </div>
        </div>
      </div>

      @if (loading()) {
        <app-loading-state message="Cargando reserva..." />
      }

      @if (error() && !loading()) {
        <app-error-state
          title="Error al cargar reserva"
          [message]="error()"
          (retry)="loadReservation()"
        />
      }

      @if (reservation(); as res) {
        <mat-card class="detail-card" appearance="outlined">
          <!-- Card header -->
          <mat-card-header>
            <mat-card-title>
              @if (res.code) {
                Reserva #{{ res.code }}
              } @else {
                Reserva — En proceso
              }
            </mat-card-title>
            <mat-card-subtitle>
              <app-status-chip [status]="displayStatus(res)" />
            </mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            <div class="info-grid">
              <div class="info-item">
                <span class="info-label">ID</span>
                <span class="info-value mono">{{ res.id }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Evento</span>
                <a class="info-value event-link" [routerLink]="['/events', res.eventId]">
                  Ver evento
                </a>
              </div>
              <div class="info-item">
                <span class="info-label">Comprador</span>
                <span class="info-value">{{ res.buyerName }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Email</span>
                <span class="info-value">{{ res.buyerEmail }}</span>
              </div>
              <div class="info-item">
                <span class="info-label">Cantidad</span>
                <span class="info-value">{{ res.quantity }} boleto(s)</span>
              </div>
              <div class="info-item">
                <span class="info-label">Código</span>
                @if (res.code) {
                  <span class="info-value mono">{{ res.code }}</span>
                } @else {
                  <span class="info-value pending-text">En proceso</span>
                }
              </div>
              <div class="info-item">
                <span class="info-label">Creada</span>
                <span class="info-value">{{ res.createdAt | date:'dd/MM/yyyy HH:mm' }}</span>
              </div>
              @if (res.expiresAt) {
                <div class="info-item">
                  <span class="info-label">Vence</span>
                  <span class="info-value">{{ res.expiresAt | date:'dd/MM/yyyy HH:mm' }}</span>
                </div>
              }
              @if (res.confirmedAt) {
                <div class="info-item">
                  <span class="info-label">Confirmada</span>
                  <span class="info-value">{{ res.confirmedAt | date:'dd/MM/yyyy HH:mm' }}</span>
                </div>
              }
              @if (res.canceledAt) {
                <div class="info-item">
                  <span class="info-label">Cancelada</span>
                  <span class="info-value">{{ res.canceledAt | date:'dd/MM/yyyy HH:mm' }}</span>
                </div>
              }
            </div>
          </mat-card-content>

          <!-- Actions -->
           @if (canUsePendingActions(res)) {
            <mat-card-actions class="detail-actions">
              <button
                mat-raised-button
                color="primary"
                [disabled]="actionLoading()"
                (click)="confirmPayment()"
              >
                <mat-icon>payments</mat-icon>
                Confirmar Pago
              </button>
              <button
                mat-stroked-button
                color="accent"
                [disabled]="actionLoading()"
                [routerLink]="['/reservations', res.id, 'edit']"
              >
                <mat-icon>edit</mat-icon>
                Editar
              </button>
              <button
                mat-stroked-button
                color="warn"
                [disabled]="actionLoading()"
                (click)="cancelReservation()"
              >
                <mat-icon>cancel</mat-icon>
                Cancelar Reserva
              </button>
            </mat-card-actions>
           }

          @if (isExpiredPending(res)) {
            <mat-card-actions class="detail-actions">
              <span class="expired-message">La reserva pendiente expiró y ya no puede confirmarse ni editarse.</span>
            </mat-card-actions>
          }

          @if (res.status === 'confirmada') {
            <mat-card-actions class="detail-actions">
              <button
                mat-stroked-button
                color="warn"
                [disabled]="actionLoading()"
                (click)="cancelReservation()"
              >
                <mat-icon>cancel</mat-icon>
                Cancelar Reserva
              </button>
            </mat-card-actions>
          }
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

    .info-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
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
      font-size: 14px;
      color: var(--mat-sys-on-surface);
      font-weight: 500;
    }

    .mono {
      font-family: 'Roboto Mono', monospace;
      font-size: 13px;
    }

    .event-link {
      color: var(--mat-sys-primary);
      text-decoration: none;
    }
    .event-link:hover {
      text-decoration: underline;
    }

    .pending-text {
      color: #e65100;
      font-weight: 600;
    }

    .detail-actions {
      display: flex;
      gap: 12px;
      padding: 16px;
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
export class ReservationDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly reservationsApi = inject(ReservationsApiService);
  private readonly notification = inject(NotificationService);

  protected readonly reservation = signal<ReservationResponse | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly actionLoading = signal(false);

  private reservationId = '';

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.reservationId = id;
        this.loadReservation();
      }
    });
  }

  protected loadReservation(): void {
    this.loading.set(true);
    this.error.set(null);

    this.reservationsApi.getById(this.reservationId).subscribe({
      next: (res) => {
        this.reservation.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar la reserva');
        this.loading.set(false);
      },
    });
  }

  protected displayStatus(reservation: ReservationResponse): string {
    return this.isExpiredPending(reservation) ? 'expirada' : reservation.status;
  }

  protected canUsePendingActions(reservation: ReservationResponse): boolean {
    return reservation.status === 'pendiente_pago' && !this.isExpiredPending(reservation);
  }

  protected async confirmPayment(): Promise<void> {
    const current = this.reservation();
    if (current && this.isExpiredPending(current)) {
      await this.notification.error('Reserva expirada', 'La reserva pendiente expiró y ya no puede confirmarse.');
      return;
    }

    const result = await this.notification.confirm(
      '¿Confirmar pago?',
      'Se generará el código oficial de la reserva (formato EV-XXXXXX) después de confirmar el pago.',
    );
    if (!result.isConfirmed) return;

    this.actionLoading.set(true);
    this.reservationsApi.confirmPayment(this.reservationId).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        const codeMsg = res.code ? ` Código: ${res.code}.` : '';
        this.notification.success('Pago confirmado', `El pago ha sido confirmado exitosamente.${codeMsg}`);
        this.router.navigate(['/reservations']);
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.notification.error('Error', err.error?.detail || 'No se pudo confirmar el pago');
      },
    });
  }

  protected async cancelReservation(): Promise<void> {
    const result = await this.notification.confirmDanger(
      '¿Cancelar reserva?',
      `Esta acción no se puede deshacer.${this.cancellationWarning()}`,
    );
    if (!result.isConfirmed) return;

    this.actionLoading.set(true);
    this.reservationsApi.cancel(this.reservationId).subscribe({
      next: (res) => {
        this.actionLoading.set(false);
        this.notification.success('Reserva cancelada', 'La reserva ha sido cancelada exitosamente');
        this.router.navigate(['/reservations']);
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.notification.error('Error', err.error?.detail || 'No se pudo cancelar la reserva');
      },
    });
  }

  private cancellationWarning(): string {
    if (this.reservation()?.status !== 'confirmada') return '';

    return ' Si faltan menos de 48 horas para el evento, la reserva quedará como perdida y los cupos no se liberarán.';
  }

  protected isExpiredPending(reservation: ReservationResponse): boolean {
    return reservation.status === 'pendiente_pago'
      && !!reservation.expiresAt
      && new Date(reservation.expiresAt).getTime() <= Date.now();
  }
}
