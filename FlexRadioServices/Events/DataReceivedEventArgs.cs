namespace FlexRadioServices.Events;

public class DataReceivedEventArgs: EventArgs
{
    public string Data { get; init; } = string.Empty;
}