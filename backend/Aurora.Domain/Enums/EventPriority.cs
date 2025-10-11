namespace Aurora.Domain.Enums;

/// <summary>
/// Represents the priority level assigned to an event.
/// </summary>
public enum EventPriority
{
    /// <summary>
    /// Lowest urgency – informational or optional events.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Default priority for regular events.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Important event that should be attended when possible.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical event that requires immediate attention.
    /// </summary>
    Critical = 4
}
