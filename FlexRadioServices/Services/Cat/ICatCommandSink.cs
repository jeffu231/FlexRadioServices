namespace FlexRadioServices.Services.Cat;

/// <summary>
/// Defines a destination for CAT commands admitted by a client session.
/// </summary>
internal interface ICatCommandSink
{
    /// <summary>
    /// Enqueues a command, waiting when the destination is at capacity.
    /// </summary>
    /// <param name="command">The complete CAT command to enqueue.</param>
    /// <param name="cancellationToken">A token that cancels the admission operation.</param>
    /// <returns>A task that completes when the command has been admitted.</returns>
    ValueTask EnqueueAsync(CatCommand command, CancellationToken cancellationToken);
}
