/**
 * Converts a PascalCase or mixed-case status string from the backend
 * into the frontend's canonical lowercase_snake_case format.
 *
 * Handles known enum values from the .NET backend:
 * ── ReservationStatus ──
 *   PendientePago → pendiente_pago
 *   Confirmada    → confirmada
 *   Cancelada     → cancelada
 *   Perdida       → perdida
 *   Expirada      → expirada
 * ── EventStatus ──
 *   Activo        → activo
 *   Cancelado     → cancelado
 *   Completado    → completado
 *
 * Any string that doesn't match a known value is converted generically:
 *   FirstWord SECOND => first_word_second
 *   Already_lower    → already_lower (pass-through)
 */
export function normalizeStatus(status: string): string {
  if (!status) return status;

  // Fast path: already lowercase snake_case or contains no uppercase
  if (!/[A-Z]/.test(status)) return status;

  // Known exact mappings (fast lookup)
  const known: Record<string, string> = {
    PendientePago: 'pendiente_pago',
    Confirmada: 'confirmada',
    Cancelada: 'cancelada',
    Perdida: 'perdida',
    Expirada: 'expirada',
    Activo: 'activo',
    Cancelado: 'cancelado',
    Completado: 'completado',
  };

  if (known[status] !== undefined) return known[status];

  // Generic PascalCase → snake_case converter
  // Insert underscore before each uppercase letter that follows a lowercase letter or digit,
  // then lowercase everything.
  return status
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
    .toLowerCase();
}
