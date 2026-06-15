import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { NotificationService } from '../../../../core/services/notification.service';
import { MatDividerModule } from '@angular/material/divider';
import { ReservationsApiService } from '../../../../core/api/reservations-api.service';
import { ReservationResponse } from '../../../../core/models/reservation.model';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { OperationalGuideComponent } from '../../../../shared/components/operational-guide/operational-guide.component';

@Component({
  selector: 'app-payment-confirm',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinner,
    MatDividerModule,
    StatusChipComponent,
    OperationalGuideComponent,
  ],
  template: `
    <div class="page-container">
      <!-- Page header -->
      <div class="page-header">
        <div>
          <h1>Confirmar Pago</h1>
          <p class="page-subtitle">Confirme el pago de una reserva pendiente</p>
        </div>
      </div>

      <app-operational-guide
        title="Guía operativa para confirmación de pago"
        description="Confirmá pagos únicamente sobre reservas pendientes y con ID verificado."
        icon="payments"
        [items]="paymentGuide"
        [badges]="paymentBadges"
      />

      <!-- Form card -->
      <mat-card class="ev-form-card ev-form-card--narrow" appearance="outlined">
        <mat-card-content>
          <form [formGroup]="confirmForm" (ngSubmit)="onSubmit()" class="ev-form">
            <mat-form-field appearance="outline" class="ev-form-field-full">
              <mat-label>ID de la reserva</mat-label>
              <input matInput formControlName="reservationId" placeholder="Ingrese el UUID de la reserva" />
              @if (confirmForm.get('reservationId')?.hasError('required')) {
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
                color="primary"
                type="submit"
                [disabled]="confirmForm.invalid || submitting()"
              >
                @if (submitting()) {
                  <mat-spinner diameter="20" />
                }
                <mat-icon>payments</mat-icon>
                <span>Confirmar Pago</span>
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
              <mat-icon color="primary">check_circle</mat-icon>
            </div>
            <mat-card-title>Pago Confirmado</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="result-grid">
              <div class="result-item">
                <span class="result-label">Estado</span>
                <app-status-chip [status]="res.status" />
              </div>
              <div class="result-item">
                <span class="result-label">Código</span>
                @if (res.code) {
                  <span class="result-value code">{{ res.code }}</span>
                } @else {
                  <span class="result-value pending-text">En proceso</span>
                }
              </div>
              <div class="result-item">
                <span class="result-label">Cantidad</span>
                <span class="result-value">{{ res.quantity }} boleto(s)</span>
              </div>
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
      min-width: 80px;
    }

    .result-value {
      font-size: 15px;
      font-weight: 500;
    }

    .code {
      font-family: 'Roboto Mono', monospace;
      font-weight: 600;
      color: var(--mat-sys-primary);
    }

    .pending-text {
      color: #e65100;
      font-weight: 600;
    }
  `,
})
export class PaymentConfirmComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly reservationsApi = inject(ReservationsApiService);
  private readonly notification = inject(NotificationService);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly result = signal<ReservationResponse | null>(null);
  protected readonly paymentBadges = ['Pago', 'Código', 'Reserva pendiente', 'Estado'];
  protected readonly paymentGuide = [
    'Ingresá el ID exacto de una reserva pendiente de pago.',
    'Al confirmar, el backend actualiza el estado y preserva el código de confirmación.',
    'Si la reserva expiró o ya fue procesada, el backend rechazará la operación.',
    'Revisá el resultado final antes de continuar con otra operación.',
  ];

  protected readonly confirmForm = this.fb.group({
    reservationId: ['', Validators.required],
  });

  protected onSubmit(): void {
    if (this.confirmForm.invalid || this.submitting()) return;

    this.submitting.set(true);
    this.error.set(null);
    this.result.set(null);

    const id = this.confirmForm.value.reservationId!;
    this.reservationsApi.confirmPayment(id).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.result.set(res);
        const codeMsg = res.code ? ` Código: ${res.code}.` : '';
        this.notification.success('Pago confirmado', `El pago ha sido confirmado exitosamente.${codeMsg}`);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error?.detail || 'Error al confirmar el pago');
      },
    });
  }
}
