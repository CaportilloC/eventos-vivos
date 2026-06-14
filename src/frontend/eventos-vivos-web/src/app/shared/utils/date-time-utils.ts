/**
 * Generates time options in 15-minute increments from 00:00 to 23:45
 * for use with mat-select time pickers.
 *
 * @example
 * generateTimeOptions()
 * // => ['00:00', '00:15', '00:30', …, '23:30', '23:45']
 */
export function generateTimeOptions(): string[] {
  const options: string[] = [];
  for (let h = 0; h < 24; h++) {
    for (let m = 0; m < 60; m += 15) {
      options.push(
        `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`,
      );
    }
  }
  return options;
}

/**
 * Combines a JavaScript Date (from Material datepicker) and a time string
 * (from mat-select or input[type="time"]) into an ISO 8601 DateTimeOffset string
 * with Colombia's offset (America/Bogotá, UTC-5, no DST).
 *
 * @example
 * toColombiaDateTimeOffset(new Date('2026-07-01'), '19:00')
 * // => "2026-07-01T19:00:00-05:00"
 */
export function toColombiaDateTimeOffset(date: Date, time: string): string {
  const [hours, minutes] = time.split(':').map(Number);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hh = String(hours).padStart(2, '0');
  const mm = String(minutes).padStart(2, '0');
  return `${year}-${month}-${day}T${hh}:${mm}:00-05:00`;
}

/**
 * Parses an ISO DateTimeOffset string back into a Date and time string
 * for use with separable datepicker + time-input form controls.
 *
 * @example
 * parseColombiaDateTimeOffset('2026-07-01T19:00:00-05:00')
 * // => { date: Date(2026-07-01), time: '19:00' }
 *
 * Returns null for falsy or unparseable input.
 */
export function parseColombiaDateTimeOffset(
  isoString: string | undefined | null,
): { date: Date; time: string } | null {
  if (!isoString) return null;
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})?$/.exec(isoString);
  if (!match) return null;

  const [, year, month, day, hours, minutes] = match;
  return {
    date: new Date(Number(year), Number(month) - 1, Number(day)),
    time: `${hours}:${minutes}`,
  };
}
