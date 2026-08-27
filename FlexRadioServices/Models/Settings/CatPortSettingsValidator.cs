using Microsoft.Extensions.Options;

namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Validates CAT TCP listener settings before the service starts.
/// </summary>
public sealed class CatPortSettingsValidator : IValidateOptions<CatPortSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CatPortSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        var profilesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var clientsById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var enabledClientCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var portsByNumber = new Dictionary<ushort, List<string>>();

        for (var profileIndex = 0; profileIndex < options.Profiles.Count; profileIndex++)
        {
            var profile = options.Profiles[profileIndex];
            var profilePath = $"{CatPortSettings.SectionName}:Profiles:{profileIndex}";
            var profileNamePath = $"{profilePath}:ProfileName";

            if (string.IsNullOrWhiteSpace(profile.ProfileName))
            {
                failures.Add($"{profileNamePath} is required.");
            }
            else if (!profilesByName.TryAdd(profile.ProfileName, profileIndex))
            {
                failures.Add($"{profileNamePath} duplicates {CatPortSettings.SectionName}:Profiles:{profilesByName[profile.ProfileName]}:ProfileName ignoring case.");
            }

            if (profile.PortSettings.Count == 0)
            {
                failures.Add($"{profilePath}:PortSettings must contain at least one port.");
            }

            for (var portIndex = 0; portIndex < profile.PortSettings.Count; portIndex++)
            {
                ValidatePort(profile.PortSettings[portIndex], $"{profilePath}:PortSettings:{portIndex}", failures, portsByNumber);
            }
        }

        for (var clientIndex = 0; clientIndex < options.Clients.Count; clientIndex++)
        {
            var client = options.Clients[clientIndex];
            var clientPath = $"{CatPortSettings.SectionName}:Clients:{clientIndex}";
            var clientIdPath = $"{clientPath}:ClientId";
            var profileNamePath = $"{clientPath}:ProfileName";

            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                failures.Add($"{clientIdPath} is required.");
            }
            else if (!clientsById.TryAdd(client.ClientId, clientIndex))
            {
                failures.Add($"{clientIdPath} duplicates {CatPortSettings.SectionName}:Clients:{clientsById[client.ClientId]}:ClientId ignoring case.");
            }

            if (string.IsNullOrWhiteSpace(client.ClientFriendlyName))
            {
                failures.Add($"{clientPath}:ClientFriendlyName is required.");
            }

            if (string.IsNullOrWhiteSpace(client.ProfileName))
            {
                failures.Add($"{profileNamePath} is required.");
                continue;
            }

            if (!profilesByName.ContainsKey(client.ProfileName))
            {
                failures.Add($"{profileNamePath} references unknown profile '{client.ProfileName}'.");
                continue;
            }

            if (client.Enabled)
            {
                enabledClientCounts.TryGetValue(client.ProfileName, out var enabledClientCount);
                enabledClientCounts[client.ProfileName] = enabledClientCount + 1;
            }
        }

        foreach (var (profileName, enabledClientCount) in enabledClientCounts)
        {
            if (enabledClientCount > 1)
            {
                failures.Add($"{CatPortSettings.SectionName}:Clients contains {enabledClientCount} enabled clients for profile '{profileName}'.");
            }
        }

        foreach (var (portNumber, portPaths) in portsByNumber)
        {
            if (portPaths.Count > 1)
            {
                failures.Add($"{CatPortSettings.SectionName}: PortNumber {portNumber} is configured more than once at {string.Join(", ", portPaths)}.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidatePort(PortSettings port, string portPath, List<string> failures,
        Dictionary<ushort, List<string>> portsByNumber)
    {
        if (port.PortNumber == 0)
        {
            failures.Add($"{portPath}:PortNumber must be between 1 and 65535.");
        }
        else
        {
            if (!portsByNumber.TryGetValue(port.PortNumber, out var portPaths))
            {
                portPaths = [];
                portsByNumber.Add(port.PortNumber, portPaths);
            }

            portPaths.Add($"{portPath}:PortNumber");
        }

        if (string.IsNullOrWhiteSpace(port.PortFriendlyName))
        {
            failures.Add($"{portPath}:PortFriendlyName is required.");
        }

        if (!string.Equals(port.Protocol, "TCP", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{portPath}:Protocol must be TCP.");
        }

        ValidateSliceLetter(port.VfoASliceLetter, "VfoASliceLetter", portPath, failures);
        ValidateSliceLetter(port.VfoBSliceLetter, "VfoBSliceLetter", portPath, failures);
        if (port.PortSliceType == PortSliceType.Designated && string.IsNullOrWhiteSpace(port.VfoASliceLetter))
        {
            failures.Add($"{portPath}:VfoASliceLetter is required for a designated port.");
        }
    }

    private static void ValidateSliceLetter(string? value, string propertyName, string portPath, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Length != 1 || value[0] is < 'A' or > 'H')
        {
            failures.Add($"{portPath}:{propertyName} must be a slice letter from A through H.");
        }
    }
}
