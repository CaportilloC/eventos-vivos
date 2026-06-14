import { Injectable, inject } from '@angular/core';
import { forkJoin, map, Observable } from 'rxjs';
import { EventsApiService } from '../api/events-api.service';
import { ReservationsApiService } from '../api/reservations-api.service';

export interface DashboardMetrics {
  totalEvents: number;
  activeEvents: number;
  totalReservations: number;
  pendingReservations: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardMetricsService {
  private readonly eventsApi = inject(EventsApiService);
  private readonly reservationsApi = inject(ReservationsApiService);

  load(): Observable<DashboardMetrics> {
    return forkJoin({
      totalEvents: this.eventsApi.list(undefined, 1, 1),
      activeEvents: this.eventsApi.list({ status: 'activo' }, 1, 1),
      totalReservations: this.reservationsApi.list(undefined, 1, 1),
      pendingReservations: this.reservationsApi.list({ status: 'pendiente_pago' }, 1, 1),
    }).pipe(
      map((result) => ({
        totalEvents: result.totalEvents.totalCount,
        activeEvents: result.activeEvents.totalCount,
        totalReservations: result.totalReservations.totalCount,
        pendingReservations: result.pendingReservations.totalCount,
      })),
    );
  }
}
