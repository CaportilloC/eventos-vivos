import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ReservationsApiService } from '../../../core/api/reservations-api.service';
import { AvailableReservationEventResponse } from '../../../core/models/event.model';
import { PagedResult } from '../../../core/models/paged-result.model';
import { ReservationFilters, ReservationResponse, ReserveTicketsRequest, UpdateReservationRequest } from '../../../core/models/reservation.model';
import { NotificationService } from '../../../core/services/notification.service';
import { apiErrorMessage } from '../../../core/utils/api-error-message';

@Injectable({ providedIn: 'root' })
export class ReservationsFacade {
  private readonly reservationsApi = inject(ReservationsApiService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);

  private readonly reservationsSubject = new BehaviorSubject<ReservationResponse[]>([]);
  private readonly selectedSubject = new BehaviorSubject<ReservationResponse | null>(null);
  private readonly createdSubject = new BehaviorSubject<ReservationResponse | null>(null);
  private readonly eventsLookupSubject = new BehaviorSubject<AvailableReservationEventResponse[]>([]);
  private readonly loadingSubject = new BehaviorSubject(false);
  private readonly selectedLoadingSubject = new BehaviorSubject(false);
  private readonly submittingSubject = new BehaviorSubject(false);
  private readonly eventsLoadingSubject = new BehaviorSubject(false);
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

  readonly reservations$ = this.reservationsSubject.asObservable();
  readonly selected$ = this.selectedSubject.asObservable();
  readonly created$ = this.createdSubject.asObservable();
  readonly eventsLookup$ = this.eventsLookupSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly selectedLoading$ = this.selectedLoadingSubject.asObservable();
  readonly submitting$ = this.submittingSubject.asObservable();
  readonly eventsLoading$ = this.eventsLoadingSubject.asObservable();
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

  loadReservations(filters: ReservationFilters | undefined, pageNumber: number, pageSize: number): void {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);

    this.reservationsApi.list(filters, pageNumber, pageSize).subscribe({
      next: (result) => {
        this.applyList(result);
        this.loadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'Error al cargar reservas'));
        this.loadingSubject.next(false);
      },
    });
  }

  loadReservation(id: string): void {
    this.selectedLoadingSubject.next(true);
    this.errorSubject.next(null);
    this.submitErrorSubject.next(null);

    this.reservationsApi.getById(id).subscribe({
      next: (reservation) => {
        this.selectedSubject.next(reservation);
        this.selectedLoadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'Error al cargar la reserva'));
        this.selectedLoadingSubject.next(false);
      },
    });
  }

  loadEventsLookup(): void {
    this.eventsLoadingSubject.next(true);

    this.reservationsApi.listAvailableEvents().subscribe({
      next: (events) => {
        this.eventsLookupSubject.next(events);
        this.eventsLoadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'No se pudieron cargar los eventos'));
        this.eventsLoadingSubject.next(false);
      },
    });
  }

  reserveTickets(request: ReserveTicketsRequest): void {
    this.submittingSubject.next(true);
    this.submitErrorSubject.next(null);
    this.createdSubject.next(null);

    this.reservationsApi.reserve(request).subscribe({
      next: (reservation) => {
        this.createdSubject.next(reservation);
        this.submittingSubject.next(false);
        this.notification.success('Reserva creada', 'La reserva ha sido registrada exitosamente');
        this.router.navigate(['/reservations']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitErrorSubject.next(apiErrorMessage(error, 'Error al crear la reserva'));
        this.submittingSubject.next(false);
      },
    });
  }

  updateReservation(id: string, request: UpdateReservationRequest): void {
    this.submittingSubject.next(true);
    this.submitErrorSubject.next(null);

    this.reservationsApi.update(id, request).subscribe({
      next: (reservation) => {
        this.selectedSubject.next(reservation);
        this.submittingSubject.next(false);
        this.notification.success('Reserva actualizada', 'Los cambios han sido guardados exitosamente');
        this.router.navigate(['/reservations']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitErrorSubject.next(apiErrorMessage(error, 'Error al actualizar la reserva'));
        this.submittingSubject.next(false);
      },
    });
  }

  confirmPayment(id: string): void {
    this.runReservationAction(
      id,
      () => this.reservationsApi.confirmPayment(id),
      'No se pudo confirmar el pago',
      (reservation) => {
        const codeMsg = reservation.code ? ` Código: ${reservation.code}.` : '';
        this.notification.success('Pago confirmado', `El pago ha sido confirmado exitosamente.${codeMsg}`);
      },
    );
  }

  cancelReservation(id: string): void {
    this.runReservationAction(
      id,
      () => this.reservationsApi.cancel(id),
      'No se pudo cancelar la reserva',
      () => this.notification.success('Reserva cancelada', 'La reserva ha sido cancelada exitosamente'),
    );
  }

  private runReservationAction(
    id: string,
    action: () => ReturnType<ReservationsApiService['cancel']>,
    fallback: string,
    onSuccess: (reservation: ReservationResponse) => void,
  ): void {
    this.submittingSubject.next(true);
    this.submitErrorSubject.next(null);

    action().subscribe({
      next: (reservation) => {
        this.selectedSubject.next(reservation);
        this.reservationsSubject.next(this.reservationsSubject.value.map((item) => item.id === id ? reservation : item));
        this.submittingSubject.next(false);
        onSuccess(reservation);
      },
      error: (error: HttpErrorResponse) => {
        this.submitErrorSubject.next(apiErrorMessage(error, fallback));
        this.submittingSubject.next(false);
      },
    });
  }

  private applyList(result: PagedResult<ReservationResponse>): void {
    this.reservationsSubject.next(result.items);
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
