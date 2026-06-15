export interface EventResponse {
  id: string;
  title: string;
  description: string | null;
  type: string;
  venueId: number;
  status: EventStatus;
  startsAt: string;
  endsAt: string;
  price: number;
  maxCapacity: number;
}

export interface AvailableReservationEventResponse {
  id: string;
  title: string;
  startsAt: string;
  price: number;
  maxCapacity: number;
  occupiedTickets: number;
  availableTickets: number;
  status: EventStatus;
}

export type EventStatus = 'activo' | 'cancelado' | 'completado';

export interface CreateEventRequest {
  title: string;
  description: string | null;
  venueId: number;
  maxCapacity: number;
  startsAt: string;
  endsAt: string;
  price: number;
  type: string;
}

export interface UpdateEventRequest {
  id: string;
  title: string;
  description: string | null;
  venueId: number;
  maxCapacity: number;
  startsAt: string;
  endsAt: string;
  price: number;
  type: string;
}

export interface EventFilters {
  type?: string;
  venueId?: number;
  startsAtFrom?: string;
  startsAtTo?: string;
  status?: string;
  titleSearch?: string;
}
