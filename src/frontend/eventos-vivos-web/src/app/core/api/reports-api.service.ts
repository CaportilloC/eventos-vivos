import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OccupancyReportResponse } from '../models/occupancy-report.model';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/events`;

  getOccupancyReport(eventId: string): Observable<OccupancyReportResponse> {
    return this.http.get<OccupancyReportResponse>(
      `${this.baseUrl}/${eventId}/occupancy-report`,
    );
  }
}
