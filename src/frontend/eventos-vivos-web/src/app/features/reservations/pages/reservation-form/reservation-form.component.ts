import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { computed } from '@angular/core';
import { startWith } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { ReservationResponse, ReserveTicketsRequest } from '../../../../core/models/reservation.model';
import { ReservationsFacade } from '../../store/reservations.facade';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { OperationalGuideComponent } from '../../../../shared/components/operational-guide/operational-guide.component';

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
    OperationalGuideComponent,
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
        <app-operational-guide
          title="Guía operativa para reservas"
          description="Validá disponibilidad y datos del comprador antes de generar una reserva pendiente de pago."
          icon="confirmation_number"
          [items]="reservationCreateGuide"
          [badges]="reservationCreateBadges"
        />

        <!-- Reservation form card -->
        <mat-card class="ev-form-card" appearance="outlined">
          <mat-card-content>
            <form [formGroup]="reservationForm" (ngSubmit)="onSubmit()" class="ev-form">

              <!-- Event selector -->
              <div class="ev-form-section">
                <h3 class="ev-form-section-title">Selección de evento</h3>
                <mat-form-field appearance="outline" class="ev-form-field-full event-search-field">
                  <mat-label>Buscar evento</mat-label>
                  <mat-icon matPrefix>search</mat-icon>
                  <input
                    matInput
                    type="search"
                    placeholder="Nombre del evento..."
                    [value]="eventSearch()"
                    (input)="eventSearch.set($any($event.target).value)"
                  />
                </mat-form-field>
                <mat-form-field appearance="outline" class="ev-form-field-full">
                  <mat-label>Evento</mat-label>
                  <mat-select formControlName="eventId">
                    @for (evt of filteredAvailableEvents(); track evt.id) {
                      <mat-option [value]="evt.id">
                        {{ eventOptionLabel(evt) }}
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

              @if (selectedEvent(); as evt) {
                <div class="selected-event-summary">
                  <div>
                    <span class="summary-label">Vista previa de la reserva</span>
                    <strong>{{ evt.title }}</strong>
                  </div>
                  <div class="summary-grid">
                    <span><strong>Fecha:</strong> {{ evt.startsAt | date:'dd/MM/yyyy HH:mm' }}</span>
                    <span><strong>Precio unitario:</strong> {{ evt.price | currency:'USD':'symbol':'1.2-2' }}</span>
                    <span><strong>Ocupación:</strong> {{ occupancyLabel(evt) }}</span>
                    <span><strong>Disponibles:</strong> {{ evt.availableTickets }}</span>
                    <span><strong>Cantidad:</strong> {{ quantityValue() }}</span>
                    <span class="summary-total"><strong>Total a pagar:</strong> {{ totalToPay() | currency:'USD':'symbol':'1.2-2' }}</span>
                  </div>
                </div>
              }

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
      max-width: 980px;
      margin: 24px auto 0;
      border-radius: var(--ev-radius-md);
    }

    .ev-form-card {
      max-width: 980px;
    }

    .event-search-field {
      margin-bottom: 10px;
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

    .selected-event-summary {
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 14px 16px;
      border: 1px solid rgba(46, 55, 164, 0.12);
      border-radius: var(--ev-radius-sm);
      background: var(--ev-surface-page);
    }

    .summary-label {
      display: block;
      margin-bottom: 4px;
      color: var(--mat-sys-on-surface-variant);
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 8px 16px;
      color: var(--mat-sys-on-surface-variant);
      font-size: 13px;
    }

    .summary-total {
      color: var(--ev-primary);
      font-weight: 700;
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
  protected readonly quantityValue = signal(1);
  protected readonly eventSearch = signal('');
  protected readonly filteredAvailableEvents = computed(() => {
    const term = this.eventSearch().trim().toLowerCase();
    if (!term) return this.availableEvents();

    return this.availableEvents().filter((evt) =>
      `${evt.title} ${evt.startsAt}`.toLowerCase().includes(term),
    );
  });
  protected readonly totalToPay = computed(() => {
    const selectedEvent = this.selectedEvent();
    const quantity = this.quantityValue();
    return selectedEvent ? selectedEvent.price * quantity : 0;
  });
  protected readonly reservationCreateBadges = ['Disponibilidad', 'Pago pendiente', 'Expiración', 'USD', 'Validación backend'];
  protected readonly reservationCreateGuide = [
    'Seleccioná un evento activo y con disponibilidad suficiente.',
    'Registrá nombre y correo correctos para seguimiento de la reserva.',
    'La moneda base del proyecto es USD; el precio y total a pagar se muestran en dólares.',
    'Reglas de cantidad: mínimo 1, máximo general 100, menos de 24 h máximo 5, precio mayor a USD 100 máximo 10.',
    'No se puede reservar a menos de 1 h del inicio; si aplican varios límites, gana el más estricto.',
    'La reserva queda pendiente hasta confirmar el pago correspondiente.',
  ];

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

    this.reservationForm.controls.quantity.valueChanges
      .pipe(
        startWith(this.reservationForm.controls.quantity.value),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((quantity) => this.quantityValue.set(Number(quantity ?? 0)));

    this.reservationsFacade.eventsLookup$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateQuantityLimit(this.reservationForm.controls.eventId.value));

    this.reservationsFacade.loadEventsLookup();

    const preselectedId = this.route.snapshot.queryParamMap.get('eventId');
    if (preselectedId) {
      this.reservationForm.patchValue({ eventId: preselectedId });
    }
  }

  protected selectedEvent() {
    const eventId = this.reservationForm.controls.eventId.value;
    return this.availableEvents().find((evt) => evt.id === eventId) ?? null;
  }

  protected eventOptionLabel(evt: { title: string; startsAt: string; price: number; occupiedTickets: number; maxCapacity: number }): string {
    const start = new Date(evt.startsAt).toLocaleString('es-CO', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
    const price = new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(evt.price);

    return `${evt.title} · ${start} · ${price} · ${this.occupancyLabel(evt)}`;
  }

  protected occupancyLabel(evt: { occupiedTickets: number; maxCapacity: number }): string {
    return `${evt.occupiedTickets}/${evt.maxCapacity} ocupados`;
  }

  private updateQuantityLimit(eventId: string | null): void {
    const selectedEvent = this.availableEvents().find((evt) => evt.id === eventId);
    let max = 100;
    const limitMessages = ['Máximo general: 100 boletos por reserva.'];

    if (selectedEvent) {
      const hoursUntilStart = (new Date(selectedEvent.startsAt).getTime() - Date.now()) / 3_600_000;

      if (hoursUntilStart >= 1 && hoursUntilStart < 24) {
        max = Math.min(max, 5);
        limitMessages.push('Menos de 24 horas: máximo 5.');
      }
      if (selectedEvent.price > 100) {
        max = Math.min(max, 10);
        limitMessages.push('Precio mayor a USD 100: máximo 10.');
      }
      max = Math.min(max, selectedEvent.availableTickets);
      limitMessages.push(`Disponibles: ${selectedEvent.availableTickets}.`);
    }

    this.quantityMax.set(max);
    this.quantityMaxMessage.set(`Límite aplicado: ${max}. ${limitMessages.join(' ')}`);

    const validators = [Validators.required, Validators.min(1)];
    validators.push(Validators.max(max));

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
