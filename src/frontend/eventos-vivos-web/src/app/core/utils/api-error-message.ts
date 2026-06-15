import { HttpErrorResponse } from '@angular/common/http';

type ProblemDetailsError = {
  detail?: unknown;
  title?: unknown;
  errors?: Record<string, unknown>;
};

function firstText(value: unknown): string | null {
  if (typeof value === 'string' && value.trim()) {
    return value;
  }

  if (Array.isArray(value)) {
    const first = value.find((item): item is string => typeof item === 'string' && item.trim().length > 0);
    return first ?? null;
  }

  return null;
}

function firstValidationError(errors: ProblemDetailsError['errors']): string | null {
  if (!errors) return null;

  for (const value of Object.values(errors)) {
    const message = firstText(value);
    if (message) return message;
  }

  return null;
}

export function apiErrorMessage(error: HttpErrorResponse, fallback: string): string {
  const problem = error.error as ProblemDetailsError | undefined;

  return firstText(problem?.detail)
    ?? firstText(problem?.title)
    ?? firstValidationError(problem?.errors)
    ?? fallback;
}
