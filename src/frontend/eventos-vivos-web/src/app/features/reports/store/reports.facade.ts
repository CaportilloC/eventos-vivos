import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { EventsApiService } from '../../../core/api/events-api.service';
import { ReportsApiService } from '../../../core/api/reports-api.service';
import { EventResponse } from '../../../core/models/event.model';
import { OccupancyReportResponse } from '../../../core/models/occupancy-report.model';
import { apiErrorMessage } from '../../../core/utils/api-error-message';

@Injectable({ providedIn: 'root' })
export class ReportsFacade {
  private readonly eventsApi = inject(EventsApiService);
  private readonly reportsApi = inject(ReportsApiService);

  private readonly eventsSubject = new BehaviorSubject<EventResponse[]>([]);
  private readonly reportSubject = new BehaviorSubject<OccupancyReportResponse | null>(null);
  private readonly eventsLoadingSubject = new BehaviorSubject(false);
  private readonly loadingSubject = new BehaviorSubject(false);
  private readonly errorSubject = new BehaviorSubject<string | null>(null);

  readonly events$ = this.eventsSubject.asObservable();
  readonly report$ = this.reportSubject.asObservable();
  readonly eventsLoading$ = this.eventsLoadingSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();

  loadEvents(): void {
    this.eventsLoadingSubject.next(true);
    this.errorSubject.next(null);

    this.eventsApi.list(undefined, 1, 50).subscribe({
      next: (result) => {
        this.eventsSubject.next(result.items);
        this.eventsLoadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'No se pudieron cargar los eventos'));
        this.eventsLoadingSubject.next(false);
      },
    });
  }

  loadReport(eventId: string): void {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);
    this.reportSubject.next(null);

    this.reportsApi.getOccupancyReport(eventId).subscribe({
      next: (report) => {
        this.reportSubject.next(report);
        this.loadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'Error al cargar el reporte'));
        this.loadingSubject.next(false);
      },
    });
  }
}
