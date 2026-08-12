namespace FlexRadioServices.Services;

/// <summary>
/// Represents a failed slice update for which restoration was attempted.
/// </summary>
public sealed class SliceCommandException(string message, Exception innerException, Exception? compensationException)
    : Exception(message, innerException)
{
    /// <summary>Gets the exception raised while restoring previously changed properties, if any.</summary>
    public Exception? CompensationException { get; } = compensationException;
}
