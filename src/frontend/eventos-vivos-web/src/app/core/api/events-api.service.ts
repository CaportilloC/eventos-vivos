import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  EventResponse,
  CreateEventRequest,
  UpdateEventRequest,
  EventFilters,
} from '../models/event.model';
import { PagedResult } from '../models/paged-result.model';
import { ReservationResponse } from '../models/reservation.model';
import { normalizeStatus } from '../../shared/utils/normalize-status.util';

@Injectable({ providedIn: 'root' })
export class EventsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/events`;

  private normalizeEvent(e: EventResponse): EventResponse {
    return { ...e, status: normalizeStatus(e.status) as EventResponse['status'] };
  }

  create(request: CreateEventRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  list(filters?: EventFilters, pageNumber = 1, pageSize = 10): Observable<PagedResult<EventResponse>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (filters) {
      if (filters.type) params = params.set('type', filters.type);
      if (filters.venueId != null) params = params.set('venueId', filters.venueId);
      if (filters.startsAtFrom) params = params.set('startsAtFrom', filters.startsAtFrom);
      if (filters.startsAtTo) params = params.set('startsAtTo', filters.startsAtTo);
      if (filters.status) params = params.set('status', filters.status);
      if (filters.titleSearch) params = params.set('titleSearch', filters.titleSearch);
    }
    return this.http.get<PagedResult<EventResponse>>(this.baseUrl, { params }).pipe(
      map((result) => ({
        ...result,
        items: result.items.map((e) => this.normalizeEvent(e)),
      })),
    );
  }

  getById(id: string): Observable<EventResponse> {
    return this.http.get<EventResponse>(`${this.baseUrl}/${id}`).pipe(
      map((e) => this.normalizeEvent(e)),
    );
  }

  update(id: string, request: UpdateEventRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {});
  }

  getReservations(eventId: string, pageNumber = 1, pageSize = 10): Observable<PagedResult<ReservationResponse>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ReservationResponse>>(`${this.baseUrl}/${eventId}/reservations`, { params });
  }
}
