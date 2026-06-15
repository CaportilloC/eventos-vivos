import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorMessage } from './api-error-message';

function httpError(error: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ error, status: 400 });
}

describe('apiErrorMessage', () => {
  it('prefers ProblemDetails detail', () => {
    const message = apiErrorMessage(
      httpError({ title: 'Validation failed', detail: 'Tickets are sold out.' }),
      'Fallback message',
    );

    expect(message).toBe('Tickets are sold out.');
  });

  it('falls back to ProblemDetails title', () => {
    const message = apiErrorMessage(
      httpError({ title: 'Reservation expired.' }),
      'Fallback message',
    );

    expect(message).toBe('Reservation expired.');
  });

  it('uses the first validation error when detail and title are missing', () => {
    const message = apiErrorMessage(
      httpError({ errors: { quantity: ['Quantity must be greater than zero.'] } }),
      'Fallback message',
    );

    expect(message).toBe('Quantity must be greater than zero.');
  });

  it('uses fallback when no readable ProblemDetails message exists', () => {
    const message = apiErrorMessage(httpError({ errors: { quantity: [] } }), 'Fallback message');

    expect(message).toBe('Fallback message');
  });
});
