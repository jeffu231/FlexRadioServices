namespace FlexRadioServices.Services;

/// <summary>
/// Provides the current application readiness state.
/// </summary>
public interface IReadinessState
{
    /// <summary>
    /// Gets a value that indicates whether the application is ready to serve radio requests.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Gets the nonsecret reason that the application is not ready, if one is available.
    /// </summary>
    string? FailureDescription { get; }

    /// <summary>
    /// Marks the application ready after its required services initialize.
    /// </summary>
    void MarkReady();

    /// <summary>
    /// Marks the application not ready.
    /// </summary>
    /// <param name="failureDescription">A nonsecret explanation for the unavailable state.</param>
    void MarkUnready(string failureDescription);
}
