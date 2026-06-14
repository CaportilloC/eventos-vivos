import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorMessage(error: HttpErrorResponse, fallback: string): string {
  const detail = error.error?.detail;
  return typeof detail === 'string' && detail.trim() ? detail : fallback;
}
