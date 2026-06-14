import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { startWith } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { EventResponse } from '../../../../core/models/event.model';
import { ReservationResponse, ReserveTicketsRequest } from '../../../../core/models/reservation.model';
import { ReservationsFacade } from '../../store/reservations.facade';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';

@Component({
  selector: 'app-reservation-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinner,
    DatePipe,
    CurrencyPipe,
    StatusChipComponent,
    LoadingStateComponent,
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
            <h1>Reservar Boletos</h1>
            <p class="page-subtitle">Seleccione un evento y complete sus datos</p>
          </div>
        </div>
      </div>

      @if (loadingEvents()) {
        <app-loading-state message="Cargando eventos disponibles..." />
      } @else {
        <!-- Reservation form card -->
        <mat-card class="ev-form-card" appearance="outlined">
          <mat-card-content>
            <form [formGroup]="reservationForm" (ngSubmit)="onSubmit()" class="ev-form">

              <!-- Event selector -->
              <div class="ev-form-section">
                <h3 class="ev-form-section-title">Selección de evento</h3>
                <mat-form-field appearance="outline" class="ev-form-field-full">
                  <mat-label>Evento</mat-label>
                  <mat-select formControlName="eventId">
                    @for (evt of availableEvents(); track evt.id) {
                      <mat-option [value]="evt.id">
                        {{ evt.title }} — {{ evt.startsAt | date:'dd/MM/yyyy' }} — {{ evt.price | currency:'COP' }}
                      </mat-option>
                    }
                  </mat-select>
                  @if (reservationForm.get('eventId')?.hasError('required')) {
                    <mat-error>Seleccione un evento</mat-error>
                  }
                </mat-form-field>
              </div>

              <!-- Buyer info -->
              <div class="ev-form-section">
                <h3 class="ev-form-section-title">Datos del comprador</h3>
                <div class="ev-form-grid">
                  <mat-form-field appearance="outline" class="ev-form-field-full">
                    <mat-label>Nombre del comprador</mat-label>
                    <input matInput formControlName="buyerName" placeholder="Ana Pérez" />
                    @if (reservationForm.get('buyerName')?.hasError('required')) {
                      <mat-error>El nombre es obligatorio</mat-error>
                    }
                    @if (reservationForm.get('buyerName')?.hasError('minlength')) {
                      <mat-error>Mínimo 2 caracteres</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="ev-form-field-full">
                    <mat-label>Email del comprador</mat-label>
                    <input matInput formControlName="buyerEmail" placeholder="ana@example.com" type="email" />
                    @if (reservationForm.get('buyerEmail')?.hasError('required')) {
                      <mat-error>El email es obligatorio</mat-error>
                    }
                    @if (reservationForm.get('buyerEmail')?.hasError('email')) {
                      <mat-error>Ingrese un email válido</mat-error>
                    }
                  </mat-form-field>
                </div>
              </div>

              <!-- Quantity -->
              <div class="ev-form-section">
                <h3 class="ev-form-section-title">Cantidad</h3>
                <mat-form-field appearance="outline" class="ev-form-field-full">
                  <mat-label>Cantidad de boletos</mat-label>
                  <input matInput type="number" formControlName="quantity" min="1" [attr.max]="quantityMax() ?? null" />
                  @if (quantityMaxMessage()) {
                    <mat-hint>{{ quantityMaxMessage() }}</mat-hint>
                  }
                  @if (reservationForm.get('quantity')?.hasError('required')) {
                    <mat-error>Indique la cantidad</mat-error>
                  }
                  @if (reservationForm.get('quantity')?.hasError('min')) {
                    <mat-error>Debe reservar al menos 1 boleto</mat-error>
                  }
                  @if (reservationForm.get('quantity')?.hasError('max')) {
                    <mat-error>Máximo {{ quantityMax() }} boletos para este evento</mat-error>
                  }
                </mat-form-field>
              </div>

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
                  [disabled]="reservationForm.invalid || submitting()"
                >
                  @if (submitting()) {
                    <mat-spinner diameter="20" />
                  }
                  <mat-icon>confirmation_number</mat-icon>
                  <span>Reservar</span>
                </button>
                <button mat-button type="button" routerLink="/events">
                  <mat-icon>close</mat-icon>
                  Cancelar
                </button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      }

      <!-- Result card -->
      @if (result(); as res) {
        <mat-card class="result-card" appearance="outlined">
          <mat-card-header>
            <div class="result-header-icon">
              <mat-icon color="primary">check_circle</mat-icon>
            </div>
            <mat-card-title>Reserva creada exitosamente</mat-card-title>
            <mat-card-subtitle>ID: {{ res.id }}</mat-card-subtitle>
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
            </div>
          </mat-card-content>
          <mat-card-actions>
            <button mat-button color="primary" [routerLink]="['/reservations/confirm']">
              <mat-icon>payments</mat-icon>
              Confirmar Pago
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

    .result-card {
      max-width: 700px;
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
      gap: 16px;
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

    @media (max-width: 600px) {
    }
  `,
})
export class ReservationFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly reservationsFacade = inject(ReservationsFacade);

  protected readonly availableEvents = toSignal(this.reservationsFacade.eventsLookup$, { initialValue: [] });
  protected readonly submitting = toSignal(this.reservationsFacade.submitting$, { initialValue: false });
  protected readonly error = toSignal(this.reservationsFacade.submitError$, { initialValue: null });
  protected readonly result = toSignal(this.reservationsFacade.created$, { initialValue: null });
  protected readonly loadingEvents = toSignal(this.reservationsFacade.eventsLoading$, { initialValue: false });
  protected readonly quantityMax = signal<number | null>(null);
  protected readonly quantityMaxMessage = signal<string | null>(null);

  protected readonly reservationForm = this.fb.group({
    eventId: ['', Validators.required],
    buyerName: ['', [Validators.required, Validators.minLength(2)]],
    buyerEmail: ['', [Validators.required, Validators.email]],
    quantity: [1, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.reservationForm.controls.eventId.valueChanges
      .pipe(
        startWith(this.reservationForm.controls.eventId.value),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((eventId) => this.updateQuantityLimit(eventId));

    this.reservationsFacade.loadEventsLookup({ status: 'activo' });

    const preselectedId = this.route.snapshot.queryParamMap.get('eventId');
    if (preselectedId) {
      this.reservationForm.patchValue({ eventId: preselectedId });
    }
  }

  private updateQuantityLimit(eventId: string | null): void {
    const selectedEvent = this.availableEvents().find((evt) => evt.id === eventId);
    let max: number | null = null;
    let message: string | null = null;

    if (selectedEvent) {
      const hoursUntilStart = (new Date(selectedEvent.startsAt).getTime() - Date.now()) / 3_600_000;

      if (hoursUntilStart >= 1 && hoursUntilStart < 24) {
        max = 5;
        message = 'Este evento inicia en menos de 24 horas: máximo 5 boletos por reserva.';
      } else if (selectedEvent.price > 100) {
        max = 10;
        message = 'Por el precio del evento, máximo 10 boletos por reserva.';
      }
    }

    this.quantityMax.set(max);
    this.quantityMaxMessage.set(message);

    const validators = [Validators.required, Validators.min(1)];
    if (max !== null) validators.push(Validators.max(max));

    this.reservationForm.controls.quantity.setValidators(validators);
    this.reservationForm.controls.quantity.updateValueAndValidity({ emitEvent: false });
  }

  protected onSubmit(): void {
    if (this.reservationForm.invalid || this.submitting()) return;

    const value = this.reservationForm.getRawValue();
    const request: ReserveTicketsRequest = {
      eventId: value.eventId ?? '',
      buyerName: value.buyerName ?? '',
      buyerEmail: value.buyerEmail ?? '',
      quantity: value.quantity ?? 1,
    };

    this.reservationsFacade.reserveTickets(request);
  }
}
