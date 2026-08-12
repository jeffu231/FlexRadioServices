namespace FlexRadioServices.Services;

/// <summary>
/// Stores the application readiness state for health checks.
/// </summary>
internal sealed class ReadinessState : IReadinessState
{
    private readonly object _sync = new();
    private bool _isReady;
    private string? _failureDescription = "FlexLib has not initialized.";

    /// <inheritdoc />
    public bool IsReady
    {
        get
        {
            lock (_sync)
            {
                return _isReady;
            }
        }
    }

    /// <inheritdoc />
    public string? FailureDescription
    {
        get
        {
            lock (_sync)
            {
                return _failureDescription;
            }
        }
    }

    /// <inheritdoc />
    public void MarkReady()
    {
        lock (_sync)
        {
            _isReady = true;
            _failureDescription = null;
        }
    }

    /// <inheritdoc />
    public void MarkUnready(string failureDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureDescription);

        lock (_sync)
        {
            _isReady = false;
            _failureDescription = failureDescription;
        }
    }
}
