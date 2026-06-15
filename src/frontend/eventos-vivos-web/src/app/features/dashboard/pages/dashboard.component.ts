import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DashboardMetricsService } from '../../../core/services/dashboard-metrics.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
  ],
  template: `
    <div class="page-container">
      <!-- Page header -->
      <div class="page-header">
        <div>
          <h1>Dashboard</h1>
          <p class="page-subtitle">Panel principal de gestión de eventos y reservas</p>
        </div>
      </div>

      <!-- Metric cards -->
      <div class="metrics-grid">
        <mat-card class="metric-card" appearance="outlined">
          <mat-card-content>
            <div class="metric-content">
              <div class="metric-icon event-icon">
                <mat-icon>event</mat-icon>
              </div>
              <div class="metric-info">
                <span class="metric-value">{{ totalEvents() }}</span>
                <span class="metric-label">Eventos</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card" appearance="outlined">
          <mat-card-content>
            <div class="metric-content">
              <div class="metric-icon active-icon">
                <mat-icon>check_circle</mat-icon>
              </div>
              <div class="metric-info">
                <span class="metric-value">{{ activeEvents() }}</span>
                <span class="metric-label">Activos</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card" appearance="outlined">
          <mat-card-content>
            <div class="metric-content">
              <div class="metric-icon reserv-icon">
                <mat-icon>confirmation_number</mat-icon>
              </div>
              <div class="metric-info">
                <span class="metric-value">{{ totalReservations() }}</span>
                <span class="metric-label">Reservas</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card" appearance="outlined">
          <mat-card-content>
            <div class="metric-content">
              <div class="metric-icon pending-icon">
                <mat-icon>hourglass_empty</mat-icon>
              </div>
              <div class="metric-info">
                <span class="metric-value">{{ pendingReservations() }}</span>
                <span class="metric-label">Pendientes</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Quick actions -->
      <mat-card class="actions-card" appearance="outlined">
        <mat-card-header>
          <mat-card-title>Acciones rápidas</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <div class="action-buttons">
            <a mat-raised-button color="primary" routerLink="/events/create">
              <mat-icon>add</mat-icon>
              Nuevo Evento
            </a>
            <a mat-raised-button color="primary" routerLink="/reservations/create">
              <mat-icon>confirmation_number</mat-icon>
              Nueva Reserva
            </a>
            <a mat-stroked-button routerLink="/reports">
              <mat-icon>bar_chart</mat-icon>
              Reportes
            </a>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Info cards -->
      <div class="info-cards-row">
        <mat-card class="info-card" appearance="outlined">
          <mat-card-header>
            <mat-card-title>Gestión de Eventos</mat-card-title>
            <mat-card-subtitle>Administre los eventos de la plataforma</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p>Cree, edite y consulte eventos. Cada evento tiene un tipo, lugar, fecha, capacidad y precio definidos.</p>
          </mat-card-content>
          <mat-card-actions>
            <a mat-button color="primary" routerLink="/events">
              <mat-icon>arrow_forward</mat-icon>
              Ir a Eventos
            </a>
          </mat-card-actions>
        </mat-card>

        <mat-card class="info-card" appearance="outlined">
          <mat-card-header>
            <mat-card-title>Control de Reservas</mat-card-title>
            <mat-card-subtitle>Gestione las reservas de boletos</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p>Registre reservas, confirme pagos y administre cancelaciones. Las reservas pendientes expiran en 15 minutos.</p>
          </mat-card-content>
          <mat-card-actions>
            <a mat-button color="primary" routerLink="/reservations">
              <mat-icon>arrow_forward</mat-icon>
              Ir a Reservas
            </a>
          </mat-card-actions>
        </mat-card>

        <mat-card class="info-card" appearance="outlined">
          <mat-card-header>
            <mat-card-title>Reportes</mat-card-title>
            <mat-card-subtitle>Métricas de ocupación</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p>Consulte reportes detallados de ocupación, ingresos y disponibilidad por evento.</p>
          </mat-card-content>
          <mat-card-actions>
            <a mat-button color="primary" routerLink="/reports">
              <mat-icon>arrow_forward</mat-icon>
              Ver Reportes
            </a>
          </mat-card-actions>
        </mat-card>
      </div>

    </div>
  `,
  styles: `
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }

    .metric-card {
      border-radius: var(--ev-radius-md);
      transition: box-shadow var(--ev-transition-normal), transform var(--ev-transition-normal);

      &:hover {
        box-shadow: var(--ev-shadow-md);
        transform: translateY(-2px);
      }
    }

    .metric-card mat-card-content {
      padding: 20px;
    }

    .metric-content {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .metric-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 48px;
      height: 48px;
      border-radius: 12px;
      flex-shrink: 0;

      mat-icon {
        font-size: 24px;
        width: 24px;
        height: 24px;
        line-height: 24px;
      }
    }

    .event-icon {
      background: #e3f2fd;
      color: #1565c0;
    }

    .active-icon {
      background: #e8f5e9;
      color: #2e7d32;
    }

    .reserv-icon {
      background: #f3e5f5;
      color: #6a1b9a;
    }

    .pending-icon {
      background: #fff8e1;
      color: #e65100;
    }

    .metric-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .metric-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--ev-text-primary);
      line-height: 1.2;
    }

    .metric-label {
      font-size: 13px;
      color: var(--ev-text-secondary);
      font-weight: 500;
    }

    .actions-card {
      margin-bottom: 24px;
      border-radius: var(--ev-radius-md);
    }

    .actions-card mat-card-title {
      font-size: 16px;
      font-weight: 600;
    }

    .action-buttons {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }

    .action-buttons a {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .info-cards-row {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }

    .info-card {
      border-radius: var(--ev-radius-md);
    }

    .info-card mat-card-content p {
      margin: 0;
      color: var(--ev-text-secondary);
      font-size: 13px;
      line-height: 1.6;
    }

    @media (max-width: 768px) {
      .metrics-grid {
        grid-template-columns: repeat(2, 1fr);
      }
      .info-cards-row {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 480px) {
      .metrics-grid {
        grid-template-columns: 1fr;
      }
      .action-buttons {
        flex-direction: column;
      }

    }
  `,
})
export class DashboardComponent {
  private readonly dashboardMetrics = inject(DashboardMetricsService);

  protected readonly totalEvents = signal(0);
  protected readonly activeEvents = signal(0);
  protected readonly totalReservations = signal(0);
  protected readonly pendingReservations = signal(0);

  constructor() {
    this.loadMetrics();
  }

  private loadMetrics(): void {
    this.dashboardMetrics.load().subscribe({
      next: (metrics) => {
        this.totalEvents.set(metrics.totalEvents);
        this.activeEvents.set(metrics.activeEvents);
        this.totalReservations.set(metrics.totalReservations);
        this.pendingReservations.set(metrics.pendingReservations);
      },
    });
  }

}
