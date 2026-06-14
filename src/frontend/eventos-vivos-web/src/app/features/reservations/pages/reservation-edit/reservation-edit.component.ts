import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { EventResponse } from '../../../../core/models/event.model';
import { ReservationResponse, UpdateReservationRequest } from '../../../../core/models/reservation.model';
import { ReservationsFacade } from '../../store/reservations.facade';
import { EventsFacade } from '../../../events/store/events.facade';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';

@Component({
  selector: 'app-reservation-edit',
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
    StatusChipComponent,
    LoadingStateComponent,
  ],
  template: `
    <div class="page-container">
      <!-- Page header -->
      <div class="page-header">
        <div class="header-left">
          <button mat-icon-button [routerLink]="['/reservations', reservationId]">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <div>
            <h1>Editar Reserva</h1>
            <p class="page-subtitle">Modifique los datos de la reserva pendiente</p>
          </div>
        </div>
      </div>

      @if (loading()) {
        <app-loading-state message="Cargando datos de la reserva..." />
      }

      @if (loadError() && !loading()) {
        <div class="load-error-card">
          <mat-icon color="warn">error</mat-icon>
          <span>{{ loadError() }}</span>
          <button mat-button color="primary" (click)="loadReservation()">Reintentar</button>
        </div>
      }

      @if (reservation(); as res) {
        @if (res.status !== 'pendiente_pago') {
          <div class="not-editable-card">
            <mat-card appearance="outlined">
              <mat-card-content>
                <div class="not-editable-content">
                  <mat-icon color="warn">block</mat-icon>
                  <div>
                    <strong>Reserva no editable</strong>
                    <p>Solo las reservas con estado <em>pendiente de pago</em> pueden editarse.
                    Esta reserva está en estado <app-status-chip [status]="res.status" />.</p>
                  </div>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" [routerLink]="['/reservations', res.id]">
                  <mat-icon>visibility</mat-icon>
                  Ver detalle
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        } @else {
          <mat-card class="ev-form-card" appearance="outlined">
            <mat-card-header>
              <mat-card-title>
                @if (res.code) {
                  Reserva #{{ res.code }}
                } @else {
                  Reserva — En proceso
                }
              </mat-card-title>
              <mat-card-subtitle>
                <app-status-chip [status]="res.status" />
              </mat-card-subtitle>
            </mat-card-header>

            <mat-card-content>
              <!-- Event info (read-only) -->
              @if (event(); as evt) {
                <div class="ev-form-readonly">
                  <span class="info-label">Evento</span>
                  <span class="info-value">{{ evt.title }}</span>
                </div>
              }

              <form [formGroup]="editForm" (ngSubmit)="onSubmit()" class="ev-form">
                <!-- Buyer info -->
                <div class="ev-form-section">
                  <h3 class="ev-form-section-title">Datos del comprador</h3>
                  <div class="ev-form-grid">
                    <mat-form-field appearance="outline" class="ev-form-field-full">
                      <mat-label>Nombre del comprador</mat-label>
                      <input matInput formControlName="buyerName" placeholder="Ana Pérez" />
                      @if (editForm.get('buyerName')?.hasError('required')) {
                        <mat-error>El nombre es obligatorio</mat-error>
                      }
                      @if (editForm.get('buyerName')?.hasError('minlength')) {
                        <mat-error>Mínimo 2 caracteres</mat-error>
                      }
                      @if (editForm.get('buyerName')?.hasError('maxlength')) {
                        <mat-error>Máximo 100 caracteres</mat-error>
                      }
                    </mat-form-field>

                    <mat-form-field appearance="outline" class="ev-form-field-full">
                      <mat-label>Email del comprador</mat-label>
                      <input matInput formControlName="buyerEmail" placeholder="ana@example.com" type="email" />
                      @if (editForm.get('buyerEmail')?.hasError('required')) {
                        <mat-error>El email es obligatorio</mat-error>
                      }
                      @if (editForm.get('buyerEmail')?.hasError('email')) {
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
                    <input matInput type="number" formControlName="quantity" min="1" max="100" />
                    @if (editForm.get('quantity')?.hasError('required')) {
                      <mat-error>Indique la cantidad</mat-error>
                    }
                    @if (editForm.get('quantity')?.hasError('min')) {
                      <mat-error>Debe reservar al menos 1 boleto</mat-error>
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
                    [disabled]="editForm.invalid || editForm.pristine || submitting()"
                  >
                    @if (submitting()) {
                      <mat-spinner diameter="20" />
                    }
                    <mat-icon>save</mat-icon>
                    <span>Guardar Cambios</span>
                  </button>
                  <button mat-button type="button" [routerLink]="['/reservations', reservationId]">
                    <mat-icon>close</mat-icon>
                    Cancelar
                  </button>
                </div>
              </form>
            </mat-card-content>
          </mat-card>
        }
      }
    </div>
  `,
  styles: `
    .page-subtitle {
      margin: 4px 0 0;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
    }

    .load-error-card {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: #fbe9e7;
      color: #c62828;
      border-radius: var(--ev-radius-sm);
      margin-bottom: 24px;
    }

    .not-editable-card {
      max-width: 600px;
      margin: 0 auto;
    }

    .not-editable-content {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      padding: 8px 0;
    }

    .not-editable-content p {
      margin: 4px 0 0;
      color: var(--mat-sys-on-surface-variant);
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
      font-weight: 500;
      color: var(--mat-sys-primary);
    }

  `,
})
export class ReservationEditComponent {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly reservationsFacade = inject(ReservationsFacade);
  private readonly eventsFacade = inject(EventsFacade);

  protected readonly reservation = toSignal(this.reservationsFacade.selected$, { initialValue: null });
  protected readonly event = toSignal(this.eventsFacade.selected$, { initialValue: null });
  protected readonly loading = toSignal(this.reservationsFacade.selectedLoading$, { initialValue: false });
  protected readonly loadError = toSignal(this.reservationsFacade.error$, { initialValue: null });
  protected readonly submitting = toSignal(this.reservationsFacade.submitting$, { initialValue: false });
  protected readonly error = toSignal(this.reservationsFacade.submitError$, { initialValue: null });

  protected reservationId = '';

  protected readonly editForm = this.fb.group({
    buyerName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    buyerEmail: ['', [Validators.required, Validators.email]],
    quantity: [1, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const id = params.get('id');
        if (id) {
          this.reservationId = id;
          this.loadReservation();
        }
      });

    this.reservationsFacade.selected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        if (!res) return;
        this.editForm.patchValue({
          buyerName: res.buyerName,
          buyerEmail: res.buyerEmail,
          quantity: res.quantity,
        });
        this.eventsFacade.loadEvent(res.eventId);
      });
  }

  protected loadReservation(): void {
    this.reservationsFacade.loadReservation(this.reservationId);
  }

  protected onSubmit(): void {
    if (this.editForm.invalid || this.editForm.pristine || this.submitting()) return;

    const value = this.editForm.getRawValue();
    const request: UpdateReservationRequest = {
      reservationId: this.reservationId,
      buyerName: value.buyerName ?? '',
      buyerEmail: value.buyerEmail ?? '',
      quantity: value.quantity ?? 1,
    };

    this.reservationsFacade.updateReservation(this.reservationId, request);
  }
}
