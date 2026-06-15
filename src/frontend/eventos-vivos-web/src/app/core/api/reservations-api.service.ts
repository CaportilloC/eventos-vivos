import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ReservationResponse,
  ReserveTicketsRequest,
  UpdateReservationRequest,
  ReservationFilters,
} from '../models/reservation.model';
import { AvailableReservationEventResponse } from '../models/event.model';
import { PagedResult } from '../models/paged-result.model';
import { normalizeStatus } from '../../shared/utils/normalize-status.util';

@Injectable({ providedIn: 'root' })
export class ReservationsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reservations`;

  private normalizeReservation(r: ReservationResponse): ReservationResponse {
    return { ...r, status: normalizeStatus(r.status) };
  }

  private normalizeAvailableEvent(e: AvailableReservationEventResponse): AvailableReservationEventResponse {
    return { ...e, status: normalizeStatus(e.status) as AvailableReservationEventResponse['status'] };
  }

  listAvailableEvents(): Observable<AvailableReservationEventResponse[]> {
    return this.http.get<AvailableReservationEventResponse[]>(`${this.baseUrl}/available-events`).pipe(
      map((events) => events.map((event) => this.normalizeAvailableEvent(event))),
    );
  }

  reserve(request: ReserveTicketsRequest): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(this.baseUrl, request).pipe(
      map((r) => this.normalizeReservation(r)),
    );
  }

  list(filters?: ReservationFilters, pageNumber = 1, pageSize = 10): Observable<PagedResult<ReservationResponse>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (filters) {
      if (filters.eventId) params = params.set('eventId', filters.eventId);
      if (filters.status) params = params.set('status', filters.status);
      if (filters.buyerEmail) params = params.set('buyerEmail', filters.buyerEmail);
    }
    return this.http.get<PagedResult<ReservationResponse>>(this.baseUrl, { params }).pipe(
      map((result) => ({
        ...result,
        items: result.items.map((r) => this.normalizeReservation(r)),
      })),
    );
  }

  getById(id: string): Observable<ReservationResponse> {
    return this.http.get<ReservationResponse>(`${this.baseUrl}/${id}`).pipe(
      map((r) => this.normalizeReservation(r)),
    );
  }

  confirmPayment(id: string): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(
      `${this.baseUrl}/${id}/confirm-payment`,
      {},
    ).pipe(
      map((r) => this.normalizeReservation(r)),
    );
  }

  cancel(id: string): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(
      `${this.baseUrl}/${id}/cancel`,
      {},
    ).pipe(
      map((r) => this.normalizeReservation(r)),
    );
  }

  update(id: string, request: UpdateReservationRequest): Observable<ReservationResponse> {
    return this.http.put<ReservationResponse>(`${this.baseUrl}/${id}`, request).pipe(
      map((r) => this.normalizeReservation(r)),
    );
  }
}
