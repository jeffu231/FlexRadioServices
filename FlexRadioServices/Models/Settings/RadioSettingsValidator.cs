using Microsoft.Extensions.Options;

namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Validates radio connection settings before the service starts.
/// </summary>
public sealed class RadioSettingsValidator : IValidateOptions<RadioSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RadioSettings options) =>
        options.AutoConnect && string.IsNullOrWhiteSpace(options.PreferredRadioIdentifier)
            ? ValidateOptionsResult.Fail($"{RadioSettings.SectionName}: PreferredRadioIdentifier is required when AutoConnect is true.")
            : ValidateOptionsResult.Success;
}
