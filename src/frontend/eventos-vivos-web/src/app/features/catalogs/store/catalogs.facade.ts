import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { VenuesApiService } from '../../../core/api/venues-api.service';
import { Venue } from '../../../core/models/venue.model';
import { apiErrorMessage } from '../../../core/utils/api-error-message';

@Injectable({ providedIn: 'root' })
export class CatalogsFacade {
  private readonly venuesApi = inject(VenuesApiService);

  private readonly venuesSubject = new BehaviorSubject<Venue[]>([]);
  private readonly loadingSubject = new BehaviorSubject(false);
  private readonly errorSubject = new BehaviorSubject<string | null>(null);

  readonly venues$ = this.venuesSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();

  loadVenues(): void {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);

    this.venuesApi.getAll(1, 50).subscribe({
      next: (result) => {
        this.venuesSubject.next(result.items);
        this.loadingSubject.next(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorSubject.next(apiErrorMessage(error, 'No se pudieron cargar los catálogos'));
        this.loadingSubject.next(false);
      },
    });
  }
}
