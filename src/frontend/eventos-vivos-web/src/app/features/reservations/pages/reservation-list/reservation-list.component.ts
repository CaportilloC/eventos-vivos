import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { NotificationService } from '../../../../core/services/notification.service';
import { ReservationFilters, ReservationResponse } from '../../../../core/models/reservation.model';
import { EventsApiService } from '../../../../core/api/events-api.service';
import { EventResponse } from '../../../../core/models/event.model';
import { ReservationsFacade } from '../../store/reservations.facade';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';

@Component({
  selector: 'app-reservation-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    DatePipe,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    StatusChipComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <div class="page-container">
      <!-- Breadcrumb -->
      <nav class="breadcrumb-nav">
        <a routerLink="/dashboard">Inicio</a>
        <span class="separator">/</span>
        <span class="current">Gestión</span>
        <span class="separator">/</span>
        <span class="current">Reservas</span>
      </nav>

      <!-- Page header with toolbar -->
      <div class="admin-toolbar reservation-toolbar">
        <div class="reservation-toolbar-header">
          <div class="admin-toolbar-left">
            <h1 class="admin-title">Reservas</h1>
            <span class="admin-subtitle">Administración de reservas</span>
          </div>
          <a class="btn btn-sm btn-primary" routerLink="/reservations/create">
            <span class="material-icons" style="font-size:16px;width:16px;height:16px;">add</span>
            Nueva
          </a>
        </div>
        <div class="admin-toolbar-actions reservation-toolbar-filters">
          <div class="filter-group reservation-filter-group">
            <div class="toolbar-search">
              <span class="material-icons search-icon">search</span>
              <input
                class="form-control form-control-sm search-input"
                [(ngModel)]="emailFilter"
                placeholder="Buscar por email..."
                (input)="onFilterChange()"
              />
            </div>
            <select class="form-select form-select-sm toolbar-select" [(ngModel)]="statusFilter" (change)="onFilterChange()">
              <option value="">Todos los estados</option>
              <option value="pendiente_pago">Pendiente</option>
              <option value="confirmada">Confirmada</option>
              <option value="cancelada">Cancelada</option>
              <option value="perdida">Perdida</option>
            </select>
            <select class="form-select form-select-sm toolbar-select" [(ngModel)]="eventFilterId" (change)="onFilterChange()">
              <option value="">Todos los eventos</option>
              @for (evt of events(); track evt.id) {
                <option [value]="evt.id">{{ evt.title }}</option>
              }
            </select>
          </div>
          <div class="action-group">
            <button class="btn btn-sm btn-outline-secondary" (click)="clearFilters()" title="Limpiar filtros">
              <span class="material-icons" style="font-size:16px;width:16px;height:16px;">clear_all</span>
            </button>
            <button class="btn btn-sm btn-outline-secondary" (click)="loadReservations()" title="Actualizar">
              <span class="material-icons" style="font-size:16px;width:16px;height:16px;">refresh</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Admin card -->
      <div class="admin-card">
        <!-- Loading state -->
        @if (loading()) {
          <app-loading-state message="Cargando reservas..." />
        }

        <!-- Error state -->
        @if (error() && !loading()) {
          <app-error-state
            title="Error al cargar reservas"
            [message]="error()"
            (retry)="loadReservations()"
          />
        }

        <!-- Empty state -->
        @if (!loading() && !error() && reservations().length === 0) {
          <app-empty-state
            icon="confirmation_number"
            title="No hay reservas"
            message="No se encontraron reservas con los filtros seleccionados."
          />
        }

        <!-- Reservations table -->
        @if (!loading() && !error() && reservations().length > 0) {
          <div class="table-responsive">
            <table class="table admin-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Evento</th>
                  <th>Comprador</th>
                  <th>Email</th>
                  <th>Boletos</th>
                  <th>Estado</th>
                  <th>Creada</th>
                  <th class="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (res of reservations(); track res.id) {
                  <tr>
                    <td>
                      @if (res.code) {
                        <a class="cell-link mono" [routerLink]="['/reservations', res.id]">{{ res.code }}</a>
                      } @else {
                        <span class="pending-code">
                          <span class="material-icons" style="font-size:14px;width:14px;height:14px;vertical-align:middle;">schedule</span>
                          <span>En proceso</span>
                        </span>
                      }
                    </td>
                    <td class="cell-muted">{{ eventTitle(res.eventId) }}</td>
                    <td class="fw-500">{{ res.buyerName }}</td>
                    <td class="cell-muted">{{ res.buyerEmail }}</td>
                    <td>{{ res.quantity }}</td>
                    <td><app-status-chip [status]="displayStatus(res)" /></td>
                    <td class="cell-muted">{{ res.createdAt | date:'dd/MM/yyyy' }}</td>
                    <td class="cell-actions">
                      <a class="btn btn-sm btn-outline-info" [routerLink]="['/reservations', res.id]" title="Ver detalle">
                        <span class="material-icons" style="font-size:14px;width:14px;height:14px;">visibility</span>
                      </a>
                      @if (canEdit(res)) {
                        <a class="btn btn-sm btn-outline-primary" [routerLink]="['/reservations', res.id, 'edit']" title="Editar reserva">
                          <span class="material-icons" style="font-size:14px;width:14px;height:14px;">edit</span>
                        </a>
                      } @else {
                        <span class="btn btn-sm btn-outline-primary btn-edit-disabled" [title]="disabledActionReason(res)" [attr.aria-label]="disabledActionReason(res)">
                          <span class="material-icons" style="font-size:14px;width:14px;height:14px;">edit</span>
                        </span>
                      }
                      @if (canConfirm(res)) {
                        <button class="btn btn-sm btn-success btn-confirm-action" (click)="confirmPayment(res)" title="Confirmar pago" aria-label="Confirmar pago de la reserva">
                          <span class="material-icons" style="font-size:16px;width:16px;height:16px;">payments</span>
                          <span>Confirmar</span>
                        </button>
                      }
                      @if (res.status === 'confirmada') {
                        <span class="btn btn-sm btn-confirm-action btn-confirmada-disabled" title="Reserva confirmada" aria-label="Reserva confirmada">
                          <span class="material-icons" style="font-size:16px;width:16px;height:16px;">check_circle</span>
                          <span>Confirmada</span>
                        </span>
                      }
                      @if (canCancel(res)) {
                        <button class="btn btn-sm btn-outline-danger" (click)="cancelReservation(res)" title="Cancelar reserva">
                          <span class="material-icons" style="font-size:14px;width:14px;height:14px;">cancel</span>
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Pagination footer -->
          <div class="admin-card-footer pagination-footer">
            <div class="pagination-info">
              <span class="table-count">
                Mostrando {{ pageStart() }}-{{ pageEnd() }} de {{ totalCount() }} reserva(s)
              </span>
            </div>
            <div class="pagination-controls">
              <select
                class="form-select form-select-sm page-size-select"
                [ngModel]="pageSize()"
                (ngModelChange)="onPageSizeChange($event)"
              >
                <option [value]="10">10</option>
                <option [value]="20">20</option>
                <option [value]="50">50</option>
              </select>
              <button
                class="btn btn-sm btn-outline-secondary"
                [disabled]="!hasPreviousPage()"
                (click)="previousPage()"
              >
                Anterior
              </button>
              <button
                class="btn btn-sm btn-outline-secondary"
                [disabled]="!hasNextPage()"
                (click)="nextPage()"
              >
                Siguiente
              </button>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
    :host {
      display: block;
    }

    .mono {
      font-family: 'Roboto Mono', monospace;
      font-size: 12px;
    }

    .fw-500 {
      font-weight: 500;
    }

    .cell-link {
      color: var(--ev-primary);
      text-decoration: none;
      font-weight: 500;
    }
    .cell-link:hover {
      text-decoration: underline;
    }

    .cell-muted {
      color: var(--ev-text-secondary);
      font-size: 13px;
    }

    .cell-actions {
      text-align: center;
      white-space: nowrap;
    }

    .cell-actions .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 30px;
      height: 30px;
      padding: 0;
      margin: 0 1px;
    }

    .cell-actions .btn-confirm-action {
      width: auto;
      height: 32px;
      padding: 0 10px;
      gap: 4px;
      margin: 0 2px;
      font-size: 13px;
      font-weight: 600;
      vertical-align: middle;
    }

    .cell-actions .btn-confirm-action:hover {
      filter: brightness(1.1);
    }

    .cell-actions .btn-confirmada-disabled {
      width: auto;
      height: 32px;
      padding: 0 10px;
      gap: 4px;
      margin: 0 2px;
      font-size: 13px;
      font-weight: 600;
      vertical-align: middle;
      background: #198754 !important;
      color: #fff !important;
      opacity: 0.55;
      cursor: default;
      pointer-events: none;
      border: 1px solid #198754;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border-radius: var(--ev-radius-sm);
      user-select: none;
    }

    .cell-actions .btn-edit-disabled {
      opacity: 0.35;
      cursor: default;
      pointer-events: none;
      filter: grayscale(1);
    }

    .pending-code {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 10px 2px 8px;
      border-radius: 100px;
      font-size: 12px;
      font-weight: 600;
      line-height: 20px;
      background-color: #fff8e1;
      color: #e65100;
      white-space: nowrap;
    }

    /* ─── Pagination ──────────────────────── */
    .pagination-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: 8px;
    }

    .pagination-info {
      display: flex;
      align-items: center;
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .page-size-select {
      width: auto;
      min-width: 70px;
    }

    .reservation-toolbar {
      flex-direction: column;
      align-items: stretch;
      gap: 10px;
    }

    .reservation-toolbar-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 12px;
    }

    .reservation-toolbar-header .btn {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      white-space: nowrap;
    }

    .reservation-toolbar-filters {
      width: 100%;
      justify-content: flex-end;
    }

    .reservation-filter-group {
      flex: 1 1 auto;
    }

    @media (max-width: 575.98px) {
      .reservation-toolbar-header {
        align-items: stretch;
      }

      .reservation-toolbar-filters {
        align-items: stretch;
      }
    }
    `,
  ],
})
export class ReservationListComponent {
  private readonly reservationsFacade = inject(ReservationsFacade);
  private readonly eventsApi = inject(EventsApiService);
  private readonly notification = inject(NotificationService);

  protected readonly reservations = toSignal(this.reservationsFacade.reservations$, { initialValue: [] });
  protected readonly events = signal<EventResponse[]>([]);
  protected readonly loading = toSignal(this.reservationsFacade.loading$, { initialValue: false });
  protected readonly error = toSignal(this.reservationsFacade.error$, { initialValue: null });
  protected statusFilter = '';
  protected emailFilter = '';
  protected eventFilterId = '';

  protected readonly pageNumber = toSignal(this.reservationsFacade.pageNumber$, { initialValue: 1 });
  protected readonly pageSize = toSignal(this.reservationsFacade.pageSize$, { initialValue: 10 });
  protected readonly totalCount = toSignal(this.reservationsFacade.totalCount$, { initialValue: 0 });
  protected readonly totalPages = toSignal(this.reservationsFacade.totalPages$, { initialValue: 1 });
  protected readonly hasPreviousPage = toSignal(this.reservationsFacade.hasPreviousPage$, { initialValue: false });
  protected readonly hasNextPage = toSignal(this.reservationsFacade.hasNextPage$, { initialValue: false });
  protected readonly pageStart = toSignal(this.reservationsFacade.pageStart$, { initialValue: 0 });
  protected readonly pageEnd = toSignal(this.reservationsFacade.pageEnd$, { initialValue: 0 });

  constructor() {
    this.loadEventsCatalog();
    this.loadReservations();
  }

  protected eventTitle(eventId: string): string {
    return this.events().find((e) => e.id === eventId)?.title ?? '—';
  }

  protected displayStatus(reservation: ReservationResponse): string {
    return this.isExpiredPending(reservation) ? 'expirada' : reservation.status;
  }

  protected canEdit(reservation: ReservationResponse): boolean {
    return reservation.status === 'pendiente_pago' && !this.isExpiredPending(reservation);
  }

  protected canConfirm(reservation: ReservationResponse): boolean {
    return reservation.status === 'pendiente_pago' && !this.isExpiredPending(reservation);
  }

  protected canCancel(reservation: ReservationResponse): boolean {
    return (reservation.status === 'pendiente_pago' && !this.isExpiredPending(reservation)) || reservation.status === 'confirmada';
  }

  protected disabledActionReason(reservation: ReservationResponse): string {
    if (this.isExpiredPending(reservation)) return 'La reserva pendiente expiró y ya no puede editarse ni confirmarse.';
    return `Solo las reservas pendientes pueden editarse. Estado actual: ${reservation.status}`;
  }

  protected loadReservations(): void {
    this.loadReservationsAtPage(this.pageNumber());
  }

  protected onFilterChange(): void {
    this.loadReservationsAtPage(1);
  }

  protected clearFilters(): void {
    this.statusFilter = '';
    this.emailFilter = '';
    this.eventFilterId = '';
    this.loadReservationsAtPage(1);
  }

  protected onPageSizeChange(newSize: number): void {
    this.loadReservationsAtPage(1, Number(newSize));
  }

  protected previousPage(): void {
    if (this.hasPreviousPage()) {
      this.loadReservationsAtPage(this.pageNumber() - 1);
    }
  }

  protected nextPage(): void {
    if (this.hasNextPage()) {
      this.loadReservationsAtPage(this.pageNumber() + 1);
    }
  }

  private loadReservationsAtPage(pageNumber: number, pageSize = this.pageSize()): void {
    const filters: ReservationFilters = {};
    if (this.statusFilter) filters.status = this.statusFilter;
    if (this.emailFilter) filters.buyerEmail = this.emailFilter;
    if (this.eventFilterId) filters.eventId = this.eventFilterId;

    this.reservationsFacade.loadReservations(filters, pageNumber, pageSize);
  }

  private loadEventsCatalog(): void {
    this.eventsApi.list(undefined, 1, 100).subscribe({
      next: (result) => this.events.set(result.items),
      error: () => this.events.set([]),
    });
  }

  protected async confirmPayment(reservation: ReservationResponse): Promise<void> {
    if (!this.canConfirm(reservation)) {
      await this.notification.error('Reserva expirada', 'La reserva pendiente expiró y ya no puede confirmarse. Creá una nueva reserva si necesitás conservar cupos.');
      return;
    }

    const result = await this.notification.confirm(
      '¿Confirmar pago?',
      'Se generará el código oficial de la reserva (formato EV-XXXXXX) después de confirmar el pago.',
    );
    if (!result.isConfirmed) return;

    this.reservationsFacade.confirmPayment(reservation.id);
  }

  protected async cancelReservation(reservation: ReservationResponse): Promise<void> {
    if (!this.canCancel(reservation)) {
      await this.notification.error('Acción no disponible', 'Esta reserva ya no puede cancelarse desde este estado.');
      return;
    }

    const identifier = reservation.code || 'en proceso';
    const result = await this.notification.confirmDanger(
      '¿Cancelar reserva?',
      `Se cancelará la reserva ${identifier}. Esta acción no se puede deshacer.${this.cancellationWarning(reservation)}`,
    );
    if (!result.isConfirmed) return;

    this.reservationsFacade.cancelReservation(reservation.id);
  }

  private cancellationWarning(reservation: ReservationResponse): string {
    if (reservation.status !== 'confirmada') return '';

    return ' Si faltan menos de 48 horas para el evento, la reserva quedará como perdida y los cupos no se liberarán.';
  }

  private isExpiredPending(reservation: ReservationResponse): boolean {
    return reservation.status === 'pendiente_pago'
      && !!reservation.expiresAt
      && new Date(reservation.expiresAt).getTime() <= Date.now();
  }
}
