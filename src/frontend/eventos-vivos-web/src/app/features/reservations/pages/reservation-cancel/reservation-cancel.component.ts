import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { NotificationService } from '../../../../core/services/notification.service';
import { ReservationsApiService } from '../../../../core/api/reservations-api.service';
import { ReservationResponse } from '../../../../core/models/reservation.model';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { OperationalGuideComponent } from '../../../../shared/components/operational-guide/operational-guide.component';

@Component({
  selector: 'app-reservation-cancel',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinner,
    DatePipe,
    StatusChipComponent,
    OperationalGuideComponent,
  ],
  template: `
    <div class="page-container">
      <!-- Page header -->
      <div class="page-header">
        <div>
          <h1>Cancelar Reserva</h1>
          <p class="page-subtitle">Cancela una reserva existente ingresando su ID</p>
        </div>
      </div>

      <app-operational-guide
        title="Guía operativa para cancelación"
        description="La cancelación puede afectar el estado final de la reserva según la cercanía del evento."
        icon="cancel_schedule_send"
        [items]="cancelGuide"
        [badges]="cancelBadges"
      />

      <!-- Form card -->
      <mat-card class="ev-form-card ev-form-card--narrow" appearance="outlined">
        <mat-card-content>
          <form [formGroup]="cancelForm" (ngSubmit)="onSubmit()" class="ev-form">
            <mat-form-field appearance="outline" class="ev-form-field-full">
              <mat-label>ID de la reserva</mat-label>
              <input matInput formControlName="reservationId" placeholder="Ingrese el UUID de la reserva" />
              @if (cancelForm.get('reservationId')?.hasError('required')) {
                <mat-error>El ID de la reserva es obligatorio</mat-error>
              }
            </mat-form-field>

            @if (error()) {
              <div class="ev-form-error">
                <mat-icon>error</mat-icon>
                <span>{{ error() }}</span>
              </div>
            }

            <div class="ev-form-actions">
              <button
                mat-raised-button
                color="warn"
                type="submit"
                [disabled]="cancelForm.invalid || submitting()"
              >
                @if (submitting()) {
                  <mat-spinner diameter="20" />
                }
                <mat-icon>cancel</mat-icon>
                <span>Cancelar Reserva</span>
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>

      <!-- Result card -->
      @if (result(); as res) {
        <mat-card class="result-card" appearance="outlined">
          <mat-card-header>
            <div class="result-header-icon">
              <mat-icon [color]="res.status === 'cancelada' ? 'warn' : 'primary'">
                {{ res.status === 'cancelada' ? 'cancel' : 'info' }}
              </mat-icon>
            </div>
            <mat-card-title>
              Reserva {{ res.status === 'cancelada' ? 'Cancelada' : res.status === 'perdida' ? 'Perdida' : res.status }}
            </mat-card-title>
            <mat-card-subtitle>ID: {{ res.id }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="result-grid">
              <div class="result-item">
                <span class="result-label">Estado final</span>
                <app-status-chip [status]="res.status" />
              </div>
              <div class="result-item">
                <span class="result-label">Cantidad</span>
                <span class="result-value">{{ res.quantity }} boleto(s)</span>
              </div>
              @if (res.canceledAt) {
                <div class="result-item">
                  <span class="result-label">Cancelado en</span>
                  <span class="result-value">{{ res.canceledAt | date:'dd/MM/yyyy HH:mm' }}</span>
                </div>
              }
            </div>
          </mat-card-content>
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

    .result-card {
      max-width: 600px;
      margin: 24px auto 0;
      border-radius: var(--ev-radius-md);
    }

    .result-header-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 8px;
    }

    .result-grid {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .result-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px;
      background: var(--ev-surface-page);
      border-radius: var(--ev-radius-sm);
    }

    .result-label {
      font-size: 12px;
      font-weight: 600;
      color: var(--mat-sys-on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      min-width: 100px;
    }

    .result-value {
      font-size: 15px;
      font-weight: 500;
    }
  `,
})
export class ReservationCancelComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly reservationsApi = inject(ReservationsApiService);
  private readonly notification = inject(NotificationService);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly result = signal<ReservationResponse | null>(null);
  protected readonly cancelBadges = ['Cancelación', '48 horas', 'Penalización', 'Estado final'];
  protected readonly cancelGuide = [
    'Ingresá el ID exacto de la reserva que necesitás cancelar.',
    'Las reservas confirmadas cerca del evento pueden quedar marcadas como pérdida.',
    'El backend determina el estado final aplicando la regla de 48 horas.',
    'Esta acción debe ejecutarse solo cuando exista confirmación operativa del usuario.',
  ];

  protected readonly cancelForm = this.fb.group({
    reservationId: ['', Validators.required],
  });

  protected onSubmit(): void {
    if (this.cancelForm.invalid || this.submitting()) return;

    this.submitting.set(true);
    this.error.set(null);
    this.result.set(null);

    const id = this.cancelForm.value.reservationId!;
    this.reservationsApi.cancel(id).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.notification.success('Reserva cancelada', 'La reserva ha sido cancelada exitosamente');
        this.router.navigate(['/reservations']);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error?.detail || 'Error al cancelar la reserva');
      },
    });
  }
}
