using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Services.FlexLib;

/// <summary>
/// Defines the FlexLib global operations used by the application.
/// </summary>
public interface IFlexLibApi
{
    /// <summary>
    /// Gets the radios currently discovered by FlexLib.
    /// </summary>
    IEnumerable<Radio> Radios { get; }

    /// <summary>
    /// Occurs when FlexLib discovers a radio.
    /// </summary>
    event Action<Radio>? RadioAdded;

    /// <summary>
    /// Occurs when FlexLib removes a discovered radio.
    /// </summary>
    event Action<Radio>? RadioRemoved;

    /// <summary>
    /// Initializes the FlexLib discovery session.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Closes the FlexLib discovery session.
    /// </summary>
    void CloseSession();
}
