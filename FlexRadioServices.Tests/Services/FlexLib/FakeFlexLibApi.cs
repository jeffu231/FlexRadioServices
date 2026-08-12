using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Services.FlexLib;

namespace FlexRadioServices.Tests.Services.FlexLib;

internal sealed class FakeFlexLibApi : IFlexLibApi
{
    private Action<Radio>? _radioAdded;
    private Action<Radio>? _radioRemoved;

    public List<string> Operations { get; } = [];

    public IEnumerable<Radio> Radios { get; init; } = [];

    public Exception? InitializeException { get; init; }

    public int RadioAddedSubscriberCount { get; private set; }

    public int RadioRemovedSubscriberCount { get; private set; }

    public event Action<Radio>? RadioAdded
    {
        add
        {
            _radioAdded += value;
            RadioAddedSubscriberCount++;
            Operations.Add("SubscribeRadioAdded");
        }
        remove
        {
            _radioAdded -= value;
            RadioAddedSubscriberCount--;
            Operations.Add("UnsubscribeRadioAdded");
        }
    }

    public event Action<Radio>? RadioRemoved
    {
        add
        {
            _radioRemoved += value;
            RadioRemovedSubscriberCount++;
            Operations.Add("SubscribeRadioRemoved");
        }
        remove
        {
            _radioRemoved -= value;
            RadioRemovedSubscriberCount--;
            Operations.Add("UnsubscribeRadioRemoved");
        }
    }

    public void Initialize()
    {
        Operations.Add("Initialize");
        if (InitializeException is not null)
        {
            throw InitializeException;
        }
    }

    public void CloseSession() => Operations.Add("CloseSession");

    public void RaiseRadioAdded(Radio radio) => _radioAdded?.Invoke(radio);
}
