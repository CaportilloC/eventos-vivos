import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ReportsFacade } from '../../store/reports.facade';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';

@Component({
  selector: 'app-occupancy-report',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
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
        <span class="current">Reportes</span>
        <span class="separator">/</span>
        <span class="current">Ocupación</span>
      </nav>

      <!-- Page header -->
      <div class="admin-toolbar">
        <div class="admin-toolbar-left">
          <h1 class="admin-title">Reporte de Ocupación</h1>
          <span class="admin-subtitle">Métricas detalladas por evento</span>
        </div>
        <div class="admin-toolbar-actions">
          <button
            class="btn btn-sm btn-outline-secondary"
            (click)="loadReport()"
            [disabled]="!eventControl.value || loading()"
          >
            <span class="material-icons" style="font-size:16px;width:16px;height:16px;">refresh</span>
            Actualizar
          </button>
        </div>
      </div>

      <!-- Event selector card -->
      <mat-card class="selector-card" appearance="outlined">
        <mat-card-content>
          <div class="selector-row">
            <mat-form-field appearance="outline" class="event-selector">
              <mat-label>Seleccionar evento</mat-label>
              <mat-select [formControl]="eventControl">
                @for (evt of events(); track evt.id) {
                  <mat-option [value]="evt.id">
                    {{ evt.title }} — {{ evt.startsAt | date:'dd/MM/yyyy' }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
            <button
              mat-raised-button
              color="primary"
              (click)="loadReport()"
              [disabled]="!eventControl.value || loading()"
            >
              <mat-icon>search</mat-icon>
              Consultar
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Loading -->
      @if (loading()) {
        <app-loading-state message="Cargando reporte..." />
      }

      <!-- Error -->
      @if (error() && !loading()) {
        <app-error-state
          title="Error al cargar reporte"
          [message]="error()"
          (retry)="loadReport()"
        />
      }

      <!-- Report metrics -->
      @if (report(); as r) {
        <div class="metrics-grid">
          <mat-card class="metric-card" appearance="outlined">
            <mat-card-header>
              <div class="metric-icon status-icon">
                <mat-icon>event</mat-icon>
              </div>
              <mat-card-title>Estado</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <app-status-chip [status]="r.status" />
            </mat-card-content>
          </mat-card>

          <mat-card class="metric-card" appearance="outlined">
            <mat-card-header>
              <div class="metric-icon confirm-icon">
                <mat-icon>confirmation_number</mat-icon>
              </div>
              <mat-card-title>Confirmados</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <span class="metric-value">{{ r.confirmedTickets }}</span>
              <span class="metric-unit">boletos</span>
            </mat-card-content>
          </mat-card>

          <mat-card class="metric-card" appearance="outlined">
            <mat-card-header>
              <div class="metric-icon lost-icon">
                <mat-icon>cancel</mat-icon>
              </div>
              <mat-card-title>Perdidos</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <span class="metric-value">{{ r.lostTickets }}</span>
              <span class="metric-unit">boletos</span>
            </mat-card-content>
          </mat-card>

          <mat-card class="metric-card" appearance="outlined">
            <mat-card-header>
              <div class="metric-icon avail-icon">
                <mat-icon>event_seat</mat-icon>
              </div>
              <mat-card-title>Disponibles</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <span class="metric-value">{{ r.availableTickets }}</span>
              <span class="metric-unit">boletos</span>
            </mat-card-content>
          </mat-card>

          <!-- Highlighted metrics -->
          <mat-card class="metric-card highlight" appearance="outlined">
            <mat-card-header>
              <div class="metric-icon occ-icon">
                <mat-icon>percent</mat-icon>
              </div>
              <mat-card-title>Ocupación</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <span class="metric-value large">{{ r.occupancyPercentage | number:'1.1-1' }}%</span>
            </mat-card-content>
          </mat-card>

          <mat-card class="metric-card highlight" appearance="outlined">
            <mat-card-header>
              <div class="metric-icon rev-icon">
                <mat-icon>payments</mat-icon>
              </div>
              <mat-card-title>Ingresos</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <span class="metric-value large currency">{{ r.revenue | currency:'COP':'symbol-narrow':'1.0-0' }}</span>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- Back link -->
        <div class="report-footer">
          <a class="btn btn-sm btn-outline-primary" routerLink="/events">
            <span class="material-icons" style="font-size:16px;width:16px;height:16px;">arrow_back</span>
            Volver a Eventos
          </a>
        </div>
      }

      <!-- No selection -->
      @if (!report() && !loading() && !error()) {
        <app-empty-state
          icon="bar_chart"
          title="Seleccione un evento"
          message="Elija un evento de la lista para ver su reporte de ocupación."
        />
      }
    </div>
  `,
  styles: [
    `
    :host {
      display: block;
    }

    .selector-card {
      margin-bottom: 24px;
      border-radius: var(--ev-radius-md);
    }

    .selector-row {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .event-selector {
      flex: 1;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 16px;
    }

    .metric-card {
      text-align: center;
      border-radius: var(--ev-radius-md);
      transition: box-shadow var(--ev-transition-normal), transform var(--ev-transition-normal);

      &:hover {
        box-shadow: var(--ev-shadow-md);
        transform: translateY(-2px);
      }
    }

    .metric-card mat-card-header {
      justify-content: center;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding-top: 20px;
    }

    .metric-card mat-card-title {
      font-size: 13px;
      font-weight: 600;
      color: var(--mat-sys-on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .metric-card mat-card-content {
      padding: 8px 16px 20px;
    }

    .metric-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      border-radius: 50%;

      mat-icon {
        font-size: 22px;
        width: 22px;
        height: 22px;
      }
    }

    .status-icon { background: #e3f2fd; color: #1565c0; }
    .confirm-icon { background: #e8f5e9; color: #2e7d32; }
    .lost-icon { background: #fbe9e7; color: #c62828; }
    .avail-icon { background: #f3e5f5; color: #6a1b9a; }
    .occ-icon { background: #fff8e1; color: #e65100; }
    .rev-icon { background: #e8f5e9; color: #1b5e20; }

    .metric-value {
      display: block;
      font-size: 28px;
      font-weight: 700;
      color: var(--mat-sys-primary);
      line-height: 1.2;
    }

    .metric-value.large { font-size: 32px; }

    .metric-value.currency { font-size: 26px; }

    .metric-unit {
      display: block;
      font-size: 12px;
      color: var(--mat-sys-outline);
      margin-top: 2px;
    }

    .highlight {
      background: linear-gradient(135deg, var(--mat-sys-primary-container), #e8f0fe);

      .metric-value {
        color: var(--mat-sys-on-primary-container);
      }
    }

    .report-footer {
      margin-top: 24px;
    }

    @media (max-width: 600px) {
      .selector-row {
        flex-direction: column;
      }
      .event-selector {
        width: 100%;
      }
      .metrics-grid {
        grid-template-columns: 1fr;
      }
    }

    @media (min-width: 601px) and (max-width: 991.98px) {
      .metrics-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
    }
    `,
  ],
})
export class OccupancyReportComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly reportsFacade = inject(ReportsFacade);

  protected readonly events = toSignal(this.reportsFacade.events$, { initialValue: [] });
  protected readonly report = toSignal(this.reportsFacade.report$, { initialValue: null });
  protected readonly loading = toSignal(this.reportsFacade.loading$, { initialValue: false });
  protected readonly error = toSignal(this.reportsFacade.error$, { initialValue: null });

  protected readonly eventControl = this.fb.control('', Validators.required);

  constructor() {
    this.reportsFacade.loadEvents();

    const eventId = this.route.snapshot.paramMap.get('id');
    if (eventId) {
      this.eventControl.setValue(eventId);
      this.loadReport();
    }
  }

  protected loadReport(): void {
    const eventId = this.eventControl.value;
    if (!eventId) return;

    this.reportsFacade.loadReport(eventId);
  }
}
