import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, map } from 'rxjs';
import { EventsApiService } from '../../../core/api/events-api.service';
import { VenuesApiService } from '../../../core/api/venues-api.service';
import { CreateEventRequest, EventFilters, EventResponse, UpdateEventRequest } from '../../../core/models/event.model';
import { PagedResult } from '../../../core/models/paged-result.model';
import { Venue } from '../../../core/models/venue.model';
import { NotificationService } from '../../../core/services/notification.service';
import { apiErrorMessage } from '../../../core/utils/api-error-message';

@Injectable({ providedIn: 'root' })
export class EventsFacade {
  private readonly eventsApi = inject(EventsApiService);
  private readonly venuesApi = inject(VenuesApiService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);

  private readonly listSubject = new BehaviorSubject<PagedResult<EventResponse> | null>(null);
  private readonly selectedSubject = new BehaviorSubject<EventResponse | null>(null);
  private readonly venuesSubject = new BehaviorSubject<Venue[]>([]);
  private readonly loadingSubject = new BehaviorSubject(false);
  private readonly selectedLoadingSubject = new BehaviorSubject(false);
  private readonly submittingSubject = new BehaviorSubject(false);
  private readonly errorSubject = new BehaviorSubject<string | null>(null);
  private readonly submitErrorSubject = new BehaviorSubject<string | null>(null);
  private readonly pageNumberSubject = new BehaviorSubject(1);
  private readonly pageSizeSubject = new BehaviorSubject(10);
  private readonly totalCountSubject = new BehaviorSubject(0);
  private readonly totalPagesSubject = new BehaviorSubject(1);
  private readonly hasPreviousPageSubject = new BehaviorSubject(false);
  private readonly hasNextPageSubject = new BehaviorSubject(false);
  private readonly pageStartSubject = new BehaviorSubject(0);
  private readonly pageEndSubject = new BehaviorSubject(0);

  readonly events$ = this.listSubject.asObservable().pipe(map((list) => list?.items ?? []));
  readonly selected$ = this.selectedSubject.asObservable();
  readonly venues$ = this.venuesSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly selectedLoading$ = this.selectedLoadingSubject.asObservable();
  readonly submitting$ = this.submittingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();
  readonly submitError$ = this.submitErrorSubject.asObservable();
  readonly pageNumber$ = this.pageNumberSubject.asObservable();
  readonly pageSize$ = this.pageSizeSubject.asObservable();
  readonly totalCount$ = this.totalCountSubject.asObservable();
  readonly totalPages$ = this.totalPagesSubject.asObservable();
  readonly hasPreviousPage$ = this.hasPreviousPageSubject.asObservable();
  readonly hasNextPage$ = this.hasNextPageSubject.asObservable();
  readonly pageStart$ = this.pageStartSubject.asObservable();
  readonly pageEnd$ = this.pageEndSubject.asObservable();

  loadEvents(filters: EventFilters | undefined, pageNumber: number, pageSize: number): void {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);

    this.eventsApi.list(filters, pageNumber, pageSize).subscribe({
      next: (result) => {
        this.applyList(result);
        this.loadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'Error al cargar eventos'));
        this.loadingSubject.next(false);
      },
    });
  }

  loadEvent(id: string): void {
    this.selectedLoadingSubject.next(true);
    this.errorSubject.next(null);
    this.submitErrorSubject.next(null);

    this.eventsApi.getById(id).subscribe({
      next: (event) => {
        this.selectedSubject.next(event);
        this.selectedLoadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'Error al cargar el evento'));
        this.selectedLoadingSubject.next(false);
      },
    });
  }

  loadVenues(): void {
    this.venuesApi.getAll(1, 50).subscribe({
      next: (result) => this.venuesSubject.next(result.items),
      error: (error: HttpErrorResponse) => this.errorSubject.next(apiErrorMessage(error, 'No se pudieron cargar los lugares disponibles')),
    });
  }

  createEvent(request: CreateEventRequest): void {
    this.submittingSubject.next(true);
    this.submitErrorSubject.next(null);

    this.eventsApi.create(request).subscribe({
      next: () => {
        this.submittingSubject.next(false);
        this.notification.success('Evento creado', 'El evento ha sido registrado exitosamente');
        this.router.navigate(['/events']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitErrorSubject.next(apiErrorMessage(error, 'Error al crear el evento'));
        this.submittingSubject.next(false);
      },
    });
  }

  updateEvent(id: string, request: UpdateEventRequest): void {
    this.submittingSubject.next(true);
    this.submitErrorSubject.next(null);

    this.eventsApi.update(id, request).subscribe({
      next: () => {
        this.submittingSubject.next(false);
        this.notification.success('Evento actualizado', 'Los cambios han sido guardados exitosamente');
        this.router.navigate(['/events']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitErrorSubject.next(apiErrorMessage(error, 'Error al actualizar el evento'));
        this.submittingSubject.next(false);
      },
    });
  }

  cancelEvent(id: string): void {
    this.submittingSubject.next(true);
    this.submitErrorSubject.next(null);

    this.eventsApi.cancel(id).subscribe({
      next: () => {
        const list = this.listSubject.value;
        if (list) this.applyList({ ...list, items: list.items.map((event) => event.id === id ? { ...event, status: 'cancelado' } : event) });
        this.submittingSubject.next(false);
        this.notification.success('Evento cancelado', 'El evento ha sido cancelado exitosamente');
      },
      error: (error: HttpErrorResponse) => {
        this.submitErrorSubject.next(apiErrorMessage(error, 'No se pudo cancelar el evento'));
        this.submittingSubject.next(false);
      },
    });
  }

  private applyList(result: PagedResult<EventResponse>): void {
    this.listSubject.next(result);
    this.pageNumberSubject.next(result.pageNumber);
    this.pageSizeSubject.next(result.pageSize);
    this.totalCountSubject.next(result.totalCount);
    this.totalPagesSubject.next(result.totalPages);
    this.hasPreviousPageSubject.next(result.hasPreviousPage);
    this.hasNextPageSubject.next(result.hasNextPage);
    this.pageStartSubject.next(result.totalCount > 0 ? ((result.pageNumber - 1) * result.pageSize) + 1 : 0);
    this.pageEndSubject.next(Math.min(result.pageNumber * result.pageSize, result.totalCount));
  }
}
