export interface ReservationResponse {
  id: string;
  eventId: string;
  quantity: number;
  status: string;
  buyerName: string;
  buyerEmail: string;
  createdAt: string;
  expiresAt: string | null;
  confirmedAt: string | null;
  canceledAt: string | null;
  code: string | null;
}

export type ReservationStatus = 'pendiente_pago' | 'confirmada' | 'cancelada' | 'perdida' | string;

export interface ReservationFilters {
  eventId?: string;
  status?: string;
  buyerEmail?: string;
}

export interface ReserveTicketsRequest {
  eventId: string;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
}

export interface UpdateReservationRequest {
  reservationId: string;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
}
