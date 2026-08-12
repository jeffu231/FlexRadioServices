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
        var failures = new List<string>();
        var duplicatePorts = options.PortSettings
            .GroupBy(port => port.PortNumber)
            .Where(group => group.Key != 0 && group.Count() > 1)
            .Select(group => group.Key);

        foreach (var port in duplicatePorts)
        {
            failures.Add($"{CatPortSettings.SectionName}: PortNumber {port} is configured more than once.");
        }

        foreach (var port in options.PortSettings)
        {
            var prefix = $"{CatPortSettings.SectionName} port {port.PortNumber}";
            if (port.PortNumber == 0)
            {
                failures.Add($"{prefix}: PortNumber must be between 1 and 65535.");
            }

            if (string.IsNullOrWhiteSpace(port.PortFriendlyName))
            {
                failures.Add($"{prefix}: PortFriendlyName is required.");
            }

            if (string.IsNullOrWhiteSpace(port.ClientId))
            {
                failures.Add($"{prefix}: ClientId is required.");
            }

            if (!string.Equals(port.Protocol, "TCP", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{prefix}: Protocol must be TCP.");
            }

            ValidateSliceLetter(port.VfoASliceLetter, "VfoASliceLetter", prefix, failures);
            ValidateSliceLetter(port.VfoBSliceLetter, "VfoBSliceLetter", prefix, failures);
            if (port.PortSliceType == PortSliceType.Designated && string.IsNullOrWhiteSpace(port.VfoASliceLetter))
            {
                failures.Add($"{prefix}: VfoASliceLetter is required for a designated port.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateSliceLetter(string? value, string propertyName, string prefix, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Length != 1 || value[0] is < 'A' or > 'H')
        {
            failures.Add($"{prefix}: {propertyName} must be a slice letter from A through H.");
        }
    }
}
