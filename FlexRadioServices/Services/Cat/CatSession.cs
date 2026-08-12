using System.Text;
using FlexRadioServices.Models.Ports.Network;

namespace FlexRadioServices.Services.Cat;

/// <summary>
/// Frames CAT commands for one TCP client connection.
/// </summary>
internal sealed class CatSession(ITcpServerClient client, ICatCommandSink commandSink, ILogger logger)
{
    /// <summary>
    /// Gets the maximum number of non-terminating ASCII bytes allowed in one CAT command.
    /// </summary>
    internal const int MaxCatCommandLength = 1024;

    private readonly StringBuilder _partialCommand = new(MaxCatCommandLength);
    private bool _completed;

    /// <summary>
    /// Processes an input chunk from this session.
    /// </summary>
    /// <param name="data">The received ASCII bytes.</param>
    /// <param name="cancellationToken">A token that cancels command admission.</param>
    /// <returns>A task that completes after all complete commands are admitted.</returns>
    internal async ValueTask ProcessAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (_completed)
        {
            return;
        }

        foreach (var value in data.ToArray())
        {
            if (value is (byte)'\r' or (byte)'\n')
            {
                continue;
            }

            if (value == (byte)';')
            {
                if (_partialCommand.Length > 0)
                {
                    var command = _partialCommand.ToString();
                    _partialCommand.Clear();
                    await commandSink.EnqueueAsync(new CatCommand(command, this), cancellationToken)
                        .ConfigureAwait(false);
                }

                continue;
            }

            _partialCommand.Append((char)value);
            if (_partialCommand.Length <= MaxCatCommandLength)
            {
                continue;
            }

            _completed = true;
            logger.LogWarning("Closing CAT client {RemoteEndPoint} because an incomplete command exceeded {MaximumLength} bytes", client.RemoteEndPoint, MaxCatCommandLength);
            client.Stop();
            return;
        }
    }

    /// <summary>
    /// Sends a CAT response to this session's client.
    /// </summary>
    /// <param name="response">The CAT response to send.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>A task that completes when the response is sent.</returns>
    internal ValueTask SendAsync(string response, CancellationToken cancellationToken) =>
        client.SendAsync(Encoding.ASCII.GetBytes(response), cancellationToken);

    /// <summary>
    /// Gets a value that indicates whether the underlying client remains connected.
    /// </summary>
    internal bool Connected => client.Connected;
}
