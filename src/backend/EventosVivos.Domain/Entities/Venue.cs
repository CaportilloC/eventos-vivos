namespace EventosVivos.Domain.Entities;

/// <summary>
/// A physical venue where events are held.
/// Capacity determines the maximum tickets available for events at this venue (RN-01).
/// </summary>
public class Venue
{
    /// <summary>Venue unique identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Venue display name.</summary>
    public string Name { get; private set; }

    /// <summary>Maximum capacity (tickets). Event max capacity cannot exceed this (RN-01).</summary>
    public int Capacity { get; private set; }

    /// <summary>City where the venue is located.</summary>
    public string City { get; private set; }

    private Venue() { Name = null!; City = null!; } // EF Core

    /// <summary>Creates a new venue.</summary>
    /// <param name="id">Venue identifier.</param>
    /// <param name="name">Venue name (required).</param>
    /// <param name="capacity">Maximum capacity, must be positive.</param>
    /// <param name="city">City location (required).</param>
    public Venue(int id, string name, int capacity, string city)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Venue name is required.", nameof(name));
        if (capacity <= 0)
            throw new ArgumentException("Venue capacity must be positive.", nameof(capacity));

        Id = id;
        Name = name;
        Capacity = capacity;
        City = city ?? throw new ArgumentNullException(nameof(city));
    }
}
