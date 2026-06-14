import { Component, inject, signal, LOCALE_ID } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { provideNativeDateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE, MatDateFormats } from '@angular/material/core';
import { EventsFacade } from '../../store/events.facade';
import { CatalogsFacade } from '../../../catalogs/store/catalogs.facade';
import {
  generateTimeOptions,
  toColombiaDateTimeOffset,
} from '../../../../shared/utils/date-time-utils';

const DD_MM_YYYY_FORMATS: MatDateFormats = {
  parse: {
    dateInput: null,
  },
  display: {
    dateInput: { day: '2-digit', month: '2-digit', year: 'numeric' } as Intl.DateTimeFormatOptions,
    monthYearLabel: { year: 'numeric', month: 'short' } as Intl.DateTimeFormatOptions,
    dateA11yLabel: { day: '2-digit', month: 'long', year: 'numeric' } as Intl.DateTimeFormatOptions,
    monthYearA11yLabel: { year: 'numeric', month: 'long' } as Intl.DateTimeFormatOptions,
  },
};

@Component({
  selector: 'app-event-create',
  standalone: true,
  providers: [
    provideNativeDateAdapter(),
    { provide: LOCALE_ID, useValue: 'es-CO' },
    { provide: MAT_DATE_LOCALE, useValue: 'es-CO' },
    { provide: MAT_DATE_FORMATS, useValue: DD_MM_YYYY_FORMATS },
  ],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinner,
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
            <h1>Crear Evento</h1>
            <p class="page-subtitle">Complete los detalles para registrar un nuevo evento</p>
          </div>
        </div>
      </div>

      <!-- Form card -->
      <mat-card class="ev-form-card ev-form-card--wide" appearance="outlined">
        <mat-card-content>
          <form [formGroup]="eventForm" (ngSubmit)="onSubmit()" class="ev-form">

            <!-- Title -->
            <div class="ev-form-section">
              <h3 class="ev-form-section-title">
                <mat-icon>info</mat-icon>
                Información general
              </h3>
              <mat-form-field appearance="outline" class="ev-form-field-full">
                <mat-label>Título del evento</mat-label>
                <input matInput formControlName="title" placeholder="Ej: Angular Summit 2026" />
                @if (eventForm.get('title')?.hasError('required')) {
                  <mat-error>El título es obligatorio</mat-error>
                }
                @if (eventForm.get('title')?.hasError('minlength')) {
                  <mat-error>El título debe tener mínimo 5 caracteres</mat-error>
                }
                @if (eventForm.get('title')?.hasError('maxlength')) {
                  <mat-error>El título debe tener máximo 100 caracteres</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="ev-form-field-full">
                <mat-label>Descripción</mat-label>
                <textarea
                  matInput
                  formControlName="description"
                  rows="3"
                  placeholder="Descripción del evento..."
                ></textarea>
                <mat-hint align="end">10-500 caracteres</mat-hint>
                @if (eventForm.get('description')?.hasError('required')) {
                  <mat-error>La descripción es obligatoria</mat-error>
                }
                @if (eventForm.get('description')?.hasError('minlength')) {
                  <mat-error>La descripción debe tener mínimo 10 caracteres</mat-error>
                }
                @if (eventForm.get('description')?.hasError('maxlength')) {
                  <mat-error>La descripción debe tener máximo 500 caracteres</mat-error>
                }
              </mat-form-field>
            </div>

            <!-- Venue and type -->
            <div class="ev-form-section">
              <h3 class="ev-form-section-title">
                <mat-icon>place</mat-icon>
                Ubicación y tipo
              </h3>
              <div class="ev-form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Lugar</mat-label>
                  <mat-select formControlName="venueId">
                    @for (venue of venues(); track venue.id) {
                      <mat-option [value]="venue.id">
                        {{ venue.name }} ({{ venue.city }}) — Cap {{ venue.capacity }}
                      </mat-option>
                    }
                  </mat-select>
                  @if (eventForm.get('venueId')?.hasError('required')) {
                    <mat-error>Seleccione un lugar</mat-error>
                  }
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Tipo</mat-label>
                  <mat-select formControlName="type">
                    <mat-option value="conferencia">Conferencia</mat-option>
                    <mat-option value="taller">Taller</mat-option>
                    <mat-option value="concierto">Concierto</mat-option>
                  </mat-select>
                  @if (eventForm.get('type')?.hasError('required')) {
                    <mat-error>Seleccione un tipo</mat-error>
                  }
                </mat-form-field>
              </div>
            </div>

            <!-- Date/time -->
            <div class="ev-form-section">
              <h3 class="ev-form-section-title">
                <mat-icon>calendar_today</mat-icon>
                Fecha y hora
              </h3>

              <div class="date-block">
                <div class="date-block-header">
                  <mat-icon>play_arrow</mat-icon>
                  Inicio
                </div>
                <div class="date-time-row">
                  <mat-form-field appearance="outline" class="date-field">
                    <mat-label>Fecha inicio</mat-label>
                    <input
                      matInput
                      [matDatepicker]="startDatePicker"
                      formControlName="startsAtDate"
                      placeholder="dd/mm/aaaa"
                    />
                    <mat-datepicker-toggle matIconSuffix [for]="startDatePicker" />
                    <mat-datepicker #startDatePicker />
                    @if (eventForm.get('startsAtDate')?.hasError('required')) {
                      <mat-error>Seleccione la fecha</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="time-field">
                    <mat-label>Hora inicio</mat-label>
                    <mat-select formControlName="startsAtTime">
                      @for (opt of timeOptions; track opt) {
                        <mat-option [value]="opt">{{ opt }}</mat-option>
                      }
                    </mat-select>
                    @if (eventForm.get('startsAtTime')?.hasError('required')) {
                      <mat-error>Seleccione la hora</mat-error>
                    }
                  </mat-form-field>
                </div>
              </div>

              <div class="date-block">
                <div class="date-block-header">
                  <mat-icon>stop</mat-icon>
                  Fin
                </div>
                <div class="date-time-row">
                  <mat-form-field appearance="outline" class="date-field">
                    <mat-label>Fecha fin</mat-label>
                    <input
                      matInput
                      [matDatepicker]="endDatePicker"
                      formControlName="endsAtDate"
                      placeholder="dd/mm/aaaa"
                    />
                    <mat-datepicker-toggle matIconSuffix [for]="endDatePicker" />
                    <mat-datepicker #endDatePicker />
                    @if (eventForm.get('endsAtDate')?.hasError('required')) {
                      <mat-error>Seleccione la fecha</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="time-field">
                    <mat-label>Hora fin</mat-label>
                    <mat-select formControlName="endsAtTime">
                      @for (opt of timeOptions; track opt) {
                        <mat-option [value]="opt">{{ opt }}</mat-option>
                      }
                    </mat-select>
                    @if (eventForm.get('endsAtTime')?.hasError('required')) {
                      <mat-error>Seleccione la hora</mat-error>
                    }
                  </mat-form-field>
                </div>
              </div>

              @if (eventForm.errors?.['endBeforeStart']) {
                <div class="ev-form-error">
                  <mat-icon>event_busy</mat-icon>
                  <span>La fecha de fin debe ser posterior al inicio</span>
                </div>
              }
              @if (eventForm.errors?.['startNotFuture']) {
                <div class="ev-form-error">
                  <mat-icon>schedule</mat-icon>
                  <span>La fecha y hora de inicio debe ser futura</span>
                </div>
              }
            </div>

            <!-- Capacity and price -->
            <div class="ev-form-section">
              <h3 class="ev-form-section-title">
                <mat-icon>confirmation_number</mat-icon>
                Capacidad y precio
              </h3>
              <div class="ev-form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Capacidad máxima</mat-label>
                  <input matInput type="number" formControlName="maxCapacity" min="1" />
                  @if (eventForm.get('maxCapacity')?.hasError('required')) {
                    <mat-error>Indique la capacidad</mat-error>
                  }
                  @if (eventForm.get('maxCapacity')?.hasError('min')) {
                    <mat-error>Debe ser al menos 1</mat-error>
                  }
                </mat-form-field>

                @if (eventForm.errors?.['capacityExceedsVenue']) {
                  <div class="ev-form-error ev-grid-span-2">
                    <mat-icon>groups</mat-icon>
                    <span>La capacidad no puede superar la capacidad del lugar seleccionado</span>
                  </div>
                }

                <mat-form-field appearance="outline">
                  <mat-label>Precio ($)</mat-label>
                  <input matInput type="number" formControlName="price" min="0" step="0.01" />
                  @if (eventForm.get('price')?.hasError('required')) {
                    <mat-error>Indique el precio</mat-error>
                  }
                  @if (eventForm.get('price')?.hasError('min')) {
                    <mat-error>El precio debe ser positivo</mat-error>
                  }
                </mat-form-field>
              </div>
            </div>

            <!-- Error display -->
            @if (error()) {
              <div class="ev-form-error">
                <mat-icon>error</mat-icon>
                <span>{{ error() }}</span>
              </div>
            }

            <!-- Actions -->
            <div class="ev-form-actions">
              <button
                mat-raised-button
                color="primary"
                type="submit"
                [disabled]="eventForm.invalid || submitting()"
              >
                @if (submitting()) {
                  <mat-spinner diameter="20" />
                }
                <mat-icon>check_circle</mat-icon>
                <span>Crear Evento</span>
              </button>
              <button mat-button type="button" routerLink="/events">
                <mat-icon>close</mat-icon>
                Cancelar
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: `
    .page-subtitle {
      margin: 4px 0 0;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
    }

  `,
})
export class EventCreateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly eventsFacade = inject(EventsFacade);
  private readonly catalogsFacade = inject(CatalogsFacade);

  protected readonly venues = toSignal(this.catalogsFacade.venues$, { initialValue: [] });
  protected readonly submitting = toSignal(this.eventsFacade.submitting$, { initialValue: false });
  protected readonly error = toSignal(this.eventsFacade.submitError$, { initialValue: null });
  protected readonly timeOptions = generateTimeOptions();

  protected readonly eventForm = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(100)]],
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
    venueId: [0, [Validators.required, Validators.min(1)]],
    type: ['', Validators.required],
    startsAtDate: [null as Date | null, Validators.required],
    startsAtTime: ['', Validators.required],
    endsAtDate: [null as Date | null, Validators.required],
    endsAtTime: ['', Validators.required],
    maxCapacity: [0, [Validators.required, Validators.min(1)]],
    price: [0, [Validators.required, Validators.min(0.01)]],
  }, {
    validators: [
      this.dateRangeValidator,
      this.startFutureValidator,
      (group) => this.capacityWithinVenueValidator(group),
    ],
  });

  constructor() {
    this.catalogsFacade.loadVenues();
  }

  /** Validates that end datetime is after start datetime. */
  private dateRangeValidator(group: AbstractControl): ValidationErrors | null {
    const startDate = group.get('startsAtDate')?.value as Date | null;
    const startTime = group.get('startsAtTime')?.value as string;
    const endDate = group.get('endsAtDate')?.value as Date | null;
    const endTime = group.get('endsAtTime')?.value as string;

    if (!startDate || !startTime || !endDate || !endTime) return null;

    const start = new Date(
      startDate.getFullYear(),
      startDate.getMonth(),
      startDate.getDate(),
      ...startTime.split(':').map(Number),
    );
    const end = new Date(
      endDate.getFullYear(),
      endDate.getMonth(),
      endDate.getDate(),
      ...endTime.split(':').map(Number),
    );

    return end > start ? null : { endBeforeStart: true };
  }

  /** Validates that event start is in the future from the browser perspective. */
  private startFutureValidator(group: AbstractControl): ValidationErrors | null {
    const startDate = group.get('startsAtDate')?.value as Date | null;
    const startTime = group.get('startsAtTime')?.value as string;

    if (!startDate || !startTime) return null;

    const start = new Date(
      startDate.getFullYear(),
      startDate.getMonth(),
      startDate.getDate(),
      ...startTime.split(':').map(Number),
    );

    return start > new Date() ? null : { startNotFuture: true };
  }

  /** Validates selected capacity against the selected venue capacity for better UX. */
  private capacityWithinVenueValidator(group: AbstractControl): ValidationErrors | null {
    const venueId = Number(group.get('venueId')?.value ?? 0);
    const maxCapacity = Number(group.get('maxCapacity')?.value ?? 0);
    const venue = this.venues().find((v) => v.id === venueId);

    if (!venue || !maxCapacity) return null;

    return maxCapacity <= venue.capacity ? null : { capacityExceedsVenue: true };
  }

  protected onSubmit(): void {
    if (this.eventForm.invalid || this.submitting()) return;

    const value = this.eventForm.value;

    this.eventsFacade.createEvent({
      title: value.title!,
      description: value.description ?? null,
      venueId: value.venueId!,
      type: value.type!,
      maxCapacity: value.maxCapacity!,
      price: value.price!,
      startsAt: toColombiaDateTimeOffset(value.startsAtDate!, value.startsAtTime!),
      endsAt: toColombiaDateTimeOffset(value.endsAtDate!, value.endsAtTime!),
    });
  }
}
