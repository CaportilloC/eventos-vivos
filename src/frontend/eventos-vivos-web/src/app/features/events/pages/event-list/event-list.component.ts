import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { NotificationService } from '../../../../core/services/notification.service';
import { EventResponse, EventFilters } from '../../../../core/models/event.model';
import { EventsFacade } from '../../store/events.facade';
import { CatalogsFacade } from '../../../catalogs/store/catalogs.facade';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';
import { toColombiaDateTimeOffset } from '../../../../shared/utils/date-time-utils';

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    DatePipe,
    CurrencyPipe,
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
        <span class="current">Eventos</span>
      </nav>

      <!-- Page header with toolbar -->
      <div class="admin-toolbar event-toolbar">
        <div class="event-toolbar-header">
          <div class="admin-toolbar-left">
            <h1 class="admin-title">Eventos</h1>
            <span class="admin-subtitle">Administración de eventos</span>
          </div>
          <a class="btn btn-sm btn-primary" routerLink="/events/create">
            <span class="material-icons" style="font-size:16px;width:16px;height:16px;">add</span>
            Nuevo
          </a>
        </div>
        <div class="admin-toolbar-actions event-toolbar-filters">
          <div class="filter-group event-filter-group">
            <div class="toolbar-search">
              <span class="material-icons search-icon">search</span>
              <input
                class="form-control form-control-sm search-input"
                [(ngModel)]="filters.titleSearch"
                placeholder="Buscar por título..."
                (input)="onFilterChange()"
              />
            </div>
            <select class="form-select form-select-sm toolbar-select" [(ngModel)]="filters.type" (change)="onFilterChange()">
              <option value="">Todos los tipos</option>
              <option value="conferencia">Conferencia</option>
              <option value="taller">Taller</option>
              <option value="concierto">Concierto</option>
            </select>
            <select class="form-select form-select-sm toolbar-select" [(ngModel)]="filters.status" (change)="onFilterChange()">
              <option value="">Todos los estados</option>
              <option value="activo">Activo</option>
              <option value="completado">Completado</option>
              <option value="cancelado">Cancelado</option>
            </select>
            <select class="form-select form-select-sm toolbar-select" [(ngModel)]="venueFilterId" (change)="onFilterChange()">
              <option [value]="0">Todas las sedes</option>
              @for (v of venues(); track v.id) {
                <option [value]="v.id">{{ v.name }}</option>
              }
            </select>
            <label class="toolbar-date-control">
              <span>Desde</span>
              <input
                type="date"
                class="form-control form-control-sm toolbar-date-input"
                [(ngModel)]="startsAtFromDate"
                (change)="onDateFilterChange()"
              />
            </label>
            <label class="toolbar-date-control">
              <span>Hasta</span>
              <input
                type="date"
                class="form-control form-control-sm toolbar-date-input"
                [(ngModel)]="startsAtToDate"
                (change)="onDateFilterChange()"
              />
            </label>
          </div>
          @if (dateRangeError()) {
            <div class="filter-error">{{ dateRangeError() }}</div>
          }
          <div class="action-group">
            <button class="btn btn-sm btn-outline-secondary" (click)="clearFilters()" title="Limpiar filtros">
              <span class="material-icons" style="font-size:16px;width:16px;height:16px;">clear_all</span>
            </button>
            <button class="btn btn-sm btn-outline-secondary" (click)="loadEvents()" title="Actualizar">
              <span class="material-icons" style="font-size:16px;width:16px;height:16px;">refresh</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Admin card -->
      <div class="admin-card">
        <!-- Loading state -->
        @if (loading()) {
          <app-loading-state message="Cargando eventos..." />
        }

        <!-- Error state -->
        @if (error() && !loading()) {
          <app-error-state
            title="Error al cargar eventos"
            [message]="error()"
            (retry)="loadEvents()"
          />
        }

        <!-- Empty state -->
        @if (!loading() && !error() && events().length === 0) {
          <app-empty-state
            icon="event_busy"
            title="No hay eventos"
            message="No se encontraron eventos con los filtros seleccionados."
          />
        }

        <!-- Events table -->
        @if (!loading() && !error() && events().length > 0) {
          <div class="table-responsive">
            <table class="table admin-table">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>Lugar</th>
                  <th>Tipo</th>
                  <th>Fecha inicio</th>
                  <th>Hora inicio</th>
                  <th>Precio</th>
                  <th>Cap.</th>
                  <th>Estado</th>
                  <th class="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (event of events(); track event.id) {
                  <tr>
                    <td>
                      <a class="cell-link" [routerLink]="['/events', event.id]">
                        {{ event.title }}
                      </a>
                    </td>
                    <td class="cell-muted">{{ venueName(event.venueId) }}</td>
                    <td><span class="type-badge">{{ event.type }}</span></td>
                    <td class="cell-muted">{{ event.startsAt | date:'dd/MM/yyyy' }}</td>
                    <td class="cell-muted">{{ event.startsAt | date:'HH:mm' }}</td>
                    <td class="cell-price">{{ event.price | currency:'USD':'symbol':'1.2-2' }}</td>
                    <td class="cell-muted">{{ event.maxCapacity }}</td>
                    <td><app-status-chip [status]="event.status" /></td>
                    <td class="cell-actions">
                      <a class="btn btn-sm btn-outline-info" [routerLink]="['/events', event.id]" title="Ver detalle">
                        <span class="material-icons" style="font-size:14px;width:14px;height:14px;">visibility</span>
                      </a>
                      @if (event.status === 'activo') {
                        <a class="btn btn-sm btn-outline-primary" [routerLink]="['/events', event.id, 'edit']" title="Editar">
                          <span class="material-icons" style="font-size:14px;width:14px;height:14px;">edit</span>
                        </a>
                        <button class="btn btn-sm btn-outline-danger" (click)="cancelEvent(event)" title="Cancelar evento">
                          <span class="material-icons" style="font-size:14px;width:14px;height:14px;">cancel</span>
                        </button>
                      }
                      <a class="btn btn-sm btn-outline-success" [routerLink]="['/reservations']" [queryParams]="{ eventId: event.id }" title="Ver reservas">
                        <span class="material-icons" style="font-size:14px;width:14px;height:14px;">confirmation_number</span>
                      </a>
                      <a class="btn btn-sm btn-outline-warning" [routerLink]="['/reports', event.id]" title="Reporte de ocupación">
                        <span class="material-icons" style="font-size:14px;width:14px;height:14px;">bar_chart</span>
                      </a>
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
                Mostrando {{ pageStart() }}-{{ pageEnd() }} de {{ totalCount() }} evento(s)
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

    /* ─── Type badge ──────────────────────── */
    .type-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 4px;
      background: #eef2ff;
      color: #2e37a4;
      font-size: 12px;
      font-weight: 500;
      text-transform: capitalize;
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

    .cell-price {
      font-weight: 600;
      color: var(--ev-primary);
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

    .event-toolbar {
      flex-direction: column;
      align-items: stretch;
      gap: 10px;
    }

    .event-toolbar-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 12px;
    }

    .event-toolbar-header .btn {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      white-space: nowrap;
    }

    .event-toolbar-filters {
      width: 100%;
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 10px;
    }

    .event-filter-group {
      display: grid;
      grid-template-columns: minmax(220px, 1.4fr) repeat(3, minmax(132px, 0.8fr)) repeat(2, minmax(170px, 1fr));
      align-items: end;
      gap: 8px;
      min-width: 0;
      flex: 1 1 auto;
    }

    .event-filter-group > .toolbar-search,
    .event-filter-group > .toolbar-select,
    .event-filter-group > .toolbar-date-control {
      min-width: 0;
      width: 100%;
      max-width: none;
    }

    .toolbar-date-control {
      display: flex;
      align-items: center;
      gap: 6px;
      margin: 0;
      color: var(--ev-text-secondary);
      font-size: 12px;
      white-space: nowrap;
    }

    .toolbar-date-input {
      width: 100%;
      height: 31px;
      font-size: 13px;
      padding-top: 0;
      padding-bottom: 0;
    }

    .filter-error {
      color: #dc3545;
      font-size: 12px;
      margin-top: 4px;
      width: 100%;
      text-align: right;
    }

    @media (max-width: 1399.98px) {
      .event-filter-group {
        grid-template-columns: minmax(240px, 2fr) repeat(3, minmax(140px, 1fr));
      }

      .toolbar-date-control {
        flex-direction: column;
        align-items: stretch;
        gap: 2px;
      }
    }

    @media (max-width: 991.98px) {
      .event-toolbar-filters {
        flex-direction: column;
        align-items: stretch;
      }

      .event-filter-group {
        grid-template-columns: repeat(2, minmax(0, 1fr));
        width: 100%;
      }

      .event-filter-group > .toolbar-search {
        grid-column: 1 / -1;
      }

      .event-toolbar-filters .action-group {
        justify-content: flex-end;
      }
    }

    @media (max-width: 575.98px) {
      .event-toolbar-header {
        align-items: stretch;
      }

      .event-toolbar-filters {
        align-items: stretch;
      }

      .event-filter-group {
        grid-template-columns: 1fr;
        width: 100%;
      }

      .event-filter-group > .toolbar-search,
      .event-filter-group > .toolbar-select,
      .event-filter-group > .toolbar-date-control {
        flex: 1 1 100%;
        width: 100%;
      }

      .toolbar-date-control {
        align-items: stretch;
        flex-direction: column;
        gap: 2px;
      }

      .toolbar-date-input {
        width: 100%;
      }
    }
    `,
  ],
})
export class EventListComponent {
  private readonly eventsFacade = inject(EventsFacade);
  private readonly catalogsFacade = inject(CatalogsFacade);
  private readonly notification = inject(NotificationService);

  protected readonly events = toSignal(this.eventsFacade.events$, { initialValue: [] });
  protected readonly venues = toSignal(this.catalogsFacade.venues$, { initialValue: [] });
  protected readonly loading = toSignal(this.eventsFacade.loading$, { initialValue: false });
  protected readonly error = toSignal(this.eventsFacade.error$, { initialValue: null });
  protected readonly dateRangeError = signal<string | null>(null);
  protected readonly filters: EventFilters = { status: 'activo' };
  protected venueFilterId = 0;
  protected startsAtFromDate = '';
  protected startsAtToDate = '';

  protected readonly pageNumber = toSignal(this.eventsFacade.pageNumber$, { initialValue: 1 });
  protected readonly pageSize = toSignal(this.eventsFacade.pageSize$, { initialValue: 10 });
  protected readonly totalCount = toSignal(this.eventsFacade.totalCount$, { initialValue: 0 });
  protected readonly totalPages = toSignal(this.eventsFacade.totalPages$, { initialValue: 1 });
  protected readonly hasPreviousPage = toSignal(this.eventsFacade.hasPreviousPage$, { initialValue: false });
  protected readonly hasNextPage = toSignal(this.eventsFacade.hasNextPage$, { initialValue: false });
  protected readonly pageStart = toSignal(this.eventsFacade.pageStart$, { initialValue: 0 });
  protected readonly pageEnd = toSignal(this.eventsFacade.pageEnd$, { initialValue: 0 });

  constructor() {
    this.catalogsFacade.loadVenues();
    this.loadEvents();
  }

  protected venueName(venueId: number): string {
    return this.venues().find((v) => v.id === venueId)?.name ?? `ID #${venueId}`;
  }

  protected loadEvents(): void {
    if (!this.syncDateFilters()) return;

    const filters: EventFilters = { ...this.filters };
    if (this.venueFilterId > 0) filters.venueId = this.venueFilterId;

    this.eventsFacade.loadEvents(filters, this.pageNumber(), this.pageSize());
  }

  protected onFilterChange(): void {
    this.loadEventsAtPage(1);
  }

  protected onDateFilterChange(): void {
    this.loadEventsAtPage(1);
  }

  protected clearFilters(): void {
    this.filters.type = undefined;
    this.filters.status = 'activo';
    this.filters.titleSearch = undefined;
    this.filters.startsAtFrom = undefined;
    this.filters.startsAtTo = undefined;
    this.venueFilterId = 0;
    this.startsAtFromDate = '';
    this.startsAtToDate = '';
    this.dateRangeError.set(null);
    this.loadEventsAtPage(1);
  }

  private syncDateFilters(): boolean {
    this.filters.startsAtFrom = undefined;
    this.filters.startsAtTo = undefined;

    const fromDate = this.parseDateInput(this.startsAtFromDate);
    const toDate = this.parseDateInput(this.startsAtToDate);

    if (fromDate && toDate) {
      if (toDate < fromDate) {
        this.dateRangeError.set('La fecha final debe ser posterior o igual a la inicial.');
        return false;
      }
    }

    this.dateRangeError.set(null);
    if (fromDate) {
      this.filters.startsAtFrom = toColombiaDateTimeOffset(fromDate, '00:00');
    }
    if (toDate) {
      this.filters.startsAtTo = toColombiaDateTimeOffset(toDate, '23:59');
    }

    return true;
  }

  private parseDateInput(value: string): Date | null {
    if (!value) return null;
    const [year, month, day] = value.split('-').map(Number);
    if (!year || !month || !day) return null;
    return new Date(year, month - 1, day);
  }

  protected onPageSizeChange(newSize: number): void {
    this.loadEventsAtPage(1, Number(newSize));
  }

  protected previousPage(): void {
    if (this.hasPreviousPage()) {
      this.loadEventsAtPage(this.pageNumber() - 1);
    }
  }

  protected nextPage(): void {
    if (this.hasNextPage()) {
      this.loadEventsAtPage(this.pageNumber() + 1);
    }
  }

  private loadEventsAtPage(pageNumber: number, pageSize = this.pageSize()): void {
    if (!this.syncDateFilters()) return;
    const filters: EventFilters = { ...this.filters };
    if (this.venueFilterId > 0) filters.venueId = this.venueFilterId;
    this.eventsFacade.loadEvents(filters, pageNumber, pageSize);
  }

  protected async cancelEvent(event: EventResponse): Promise<void> {
    const result = await this.notification.confirmDanger(
      '¿Cancelar evento?',
      `Se cancelará "${event.title}" y todas sus reservas asociadas. Esta acción no se puede deshacer.`,
    );
    if (!result.isConfirmed) return;

    this.eventsFacade.cancelEvent(event.id);
  }
}
