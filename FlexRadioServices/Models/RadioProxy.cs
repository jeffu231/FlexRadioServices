using Flex.Smoothlake.FlexLib;
using Flex.Util;
using Newtonsoft.Json;

namespace FlexRadioServices.Models;

public sealed class RadioProxy
{
    private readonly Radio _radio;
    public RadioProxy(Radio radio)
    {
        _radio = radio;
    }

    [JsonIgnore]
    internal Radio Radio => _radio;

    public string Model => _radio.Model ?? string.Empty;
    
    public string Nickname => _radio.Nickname ?? string.Empty;

    public string Callsign => _radio.Callsign ?? string.Empty;
    
    public string Serial => _radio.Serial ?? string.Empty;

    public string Version => FlexVersion.ToString(_radio.Version);
}