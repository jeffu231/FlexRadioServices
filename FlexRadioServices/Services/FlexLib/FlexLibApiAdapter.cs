using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Services.FlexLib;

/// <summary>
/// Adapts FlexLib's static global API for dependency-injected application services.
/// </summary>
public sealed class FlexLibApiAdapter : IFlexLibApi
{
    private bool _eventsAttached;
    private bool _initializationAttempted;

    /// <inheritdoc />
    public IEnumerable<Radio> Radios => API.RadioList.Cast<Radio>().ToArray();

    /// <inheritdoc />
    public event Action<Radio>? RadioAdded;

    /// <inheritdoc />
    public event Action<Radio>? RadioRemoved;

    /// <inheritdoc />
    public void Initialize()
    {
        if (_initializationAttempted)
        {
            return;
        }

        _initializationAttempted = true;
        API.IsGUI = false;
        API.ProgramName = "FlexRadioService";
        API.RadioAdded += OnRadioAdded;
        API.RadioRemoved += OnRadioRemoved;
        _eventsAttached = true;
        API.Init();
    }

    /// <inheritdoc />
    public void CloseSession()
    {
        if (_eventsAttached)
        {
            API.RadioAdded -= OnRadioAdded;
            API.RadioRemoved -= OnRadioRemoved;
            _eventsAttached = false;
        }

        if (_initializationAttempted)
        {
            API.CloseSession();
            _initializationAttempted = false;
        }
    }

    private void OnRadioAdded(Radio radio) => RadioAdded?.Invoke(radio);

    private void OnRadioRemoved(Radio radio) => RadioRemoved?.Invoke(radio);
}
