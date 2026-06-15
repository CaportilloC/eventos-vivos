import {
  generateTimeOptions,
  parseColombiaDateTimeOffset,
  toColombiaDateTimeOffset,
} from './date-time-utils';

describe('date-time-utils', () => {
  it('generates 15-minute time options for a full day', () => {
    const options = generateTimeOptions();

    expect(options).toHaveLength(96);
    expect(options[0]).toBe('00:00');
    expect(options[options.length - 1]).toBe('23:45');
  });

  it('creates Colombia DateTimeOffset without converting to browser timezone', () => {
    const value = toColombiaDateTimeOffset(new Date(2026, 6, 1), '19:30');

    expect(value).toBe('2026-07-01T19:30:00-05:00');
  });

  it('parses DateTimeOffset preserving calendar date and time controls', () => {
    const parsed = parseColombiaDateTimeOffset('2026-07-01T19:30:00-05:00');

    expect(parsed?.date.getFullYear()).toBe(2026);
    expect(parsed?.date.getMonth()).toBe(6);
    expect(parsed?.date.getDate()).toBe(1);
    expect(parsed?.time).toBe('19:30');
  });

  it('returns null for empty or invalid date strings', () => {
    expect(parseColombiaDateTimeOffset(null)).toBeNull();
    expect(parseColombiaDateTimeOffset('not-a-date')).toBeNull();
  });
});
