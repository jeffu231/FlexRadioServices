namespace FlexRadioServices.Models.Ports;

public interface ICatPortService: IHostedService
{
    /// <summary>
    /// Gets the task that completes when the CAT listener finishes execution.
    /// </summary>
    /// <remarks>
    /// Gets <see langword="null"/> before <see cref="IHostedService.StartAsync"/> starts the listener.
    /// </remarks>
    Task? CompletionTask { get; }
}
