import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { VenuesApiService } from '../../../../core/api/venues-api.service';
import { Venue } from '../../../../core/models/venue.model';
import { PagedResult } from '../../../../core/models/paged-result.model';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';

@Component({
  selector: 'app-venue-list',
  standalone: true,
  imports: [
    RouterLink,
    DecimalPipe,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
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
        <span class="current">Catálogos</span>
      </nav>

      <!-- Page header -->
      <div class="admin-toolbar">
        <div class="admin-toolbar-left">
          <h1 class="admin-title">Catálogos</h1>
          <span class="admin-subtitle">Lugares y sedes disponibles</span>
        </div>
        <div class="admin-toolbar-actions">
          <div class="action-group">
            <button class="btn btn-outline-secondary btn-sm" (click)="loadVenues()">
              <span class="material-icons" style="font-size:16px;width:16px;height:16px;">refresh</span>
              Actualizar
            </button>
          </div>
        </div>
      </div>

      <!-- Admin card with table -->
      <div class="admin-card">
        <!-- Loading state -->
        @if (loading()) {
          <app-loading-state message="Cargando lugares..." />
        }

        <!-- Error state -->
        @if (error() && !loading()) {
          <app-error-state
            title="Error al cargar lugares"
            [message]="error()"
            (retry)="loadVenues()"
          />
        }

        <!-- Empty state -->
        @if (!loading() && !error() && venues().length === 0) {
          <app-empty-state
            icon="location_city"
            title="Sin lugares registrados"
            message="No hay lugares disponibles en el sistema."
          />
        }

        <!-- Venues table -->
        @if (!loading() && !error() && venues().length > 0) {
          <div class="table-responsive">
            <table class="table admin-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nombre</th>
                  <th>Ciudad</th>
                  <th>Capacidad</th>
                  <th class="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (venue of venues(); track venue.id) {
                  <tr>
                    <td class="mono-cell">{{ venue.id }}</td>
                    <td class="cell-primary">{{ venue.name }}</td>
                    <td>{{ venue.city }}</td>
                    <td>{{ venue.capacity | number }} personas</td>
                    <td class="cell-actions">
                      <a
                        class="btn btn-sm btn-outline-primary"
                        [routerLink]="['/events']"
                        [queryParams]="{ venueId: venue.id }"
                        title="Ver eventos de este lugar"
                      >
                        <span class="material-icons" style="font-size:16px;width:16px;height:16px;">visibility</span>
                        Eventos
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
                Mostrando {{ pageStart() }}-{{ pageEnd() }} de {{ totalCount() }} lugar(es)
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
  styles: `
    :host {
      display: block;
    }

    .page-subtitle {
      margin: 4px 0 0;
      color: var(--ev-text-secondary);
      font-size: 14px;
    }

    .mono-cell {
      font-family: 'Roboto Mono', monospace;
      font-size: 12px;
      color: var(--ev-text-secondary);
    }

    .cell-primary {
      font-weight: 500;
      color: var(--ev-text-primary);
    }

    .cell-actions {
      text-align: center;
      white-space: nowrap;
    }

    .cell-actions .btn {
      display: inline-flex;
      align-items: center;
      gap: 4px;
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
  `,
})
export class VenueListComponent {
  private readonly venuesApi = inject(VenuesApiService);

  protected readonly venues = signal<Venue[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  // Pagination state
  protected readonly pageNumber = signal(1);
  protected readonly pageSize = signal(50);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(1);
  protected readonly hasPreviousPage = signal(false);
  protected readonly hasNextPage = signal(false);

  protected readonly pageStart = signal(0);
  protected readonly pageEnd = signal(0);

  constructor() {
    this.loadVenues();
  }

  private updatePaginationMeta(paged: PagedResult<Venue>): void {
    this.totalCount.set(paged.totalCount);
    this.totalPages.set(paged.totalPages);
    this.hasPreviousPage.set(paged.hasPreviousPage);
    this.hasNextPage.set(paged.hasNextPage);

    const start = paged.totalCount === 0 ? 0 : ((paged.pageNumber - 1) * paged.pageSize) + 1;
    const end = Math.min(paged.pageNumber * paged.pageSize, paged.totalCount);
    this.pageStart.set(start);
    this.pageEnd.set(end);
  }

  protected loadVenues(): void {
    this.loading.set(true);
    this.error.set(null);

    this.venuesApi.getAll(this.pageNumber(), this.pageSize()).subscribe({
      next: (data) => {
        this.venues.set(data.items);
        this.updatePaginationMeta(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.detail || 'Error al cargar lugares');
        this.loading.set(false);
      },
    });
  }

  protected onPageSizeChange(newSize: number): void {
    this.pageSize.set(newSize);
    this.pageNumber.set(1);
    this.loadVenues();
  }

  protected previousPage(): void {
    if (this.hasPreviousPage()) {
      this.pageNumber.update((n) => n - 1);
      this.loadVenues();
    }
  }

  protected nextPage(): void {
    if (this.hasNextPage()) {
      this.pageNumber.update((n) => n + 1);
      this.loadVenues();
    }
  }
}
