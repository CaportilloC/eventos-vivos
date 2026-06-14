export interface OccupancyReportResponse {
  eventId: string;
  status: string;
  confirmedTickets: number;
  lostTickets: number;
  availableTickets: number;
  occupancyPercentage: number;
  revenue: number;
}
