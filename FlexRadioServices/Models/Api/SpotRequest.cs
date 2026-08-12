using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FlexRadioServices.Models.Api;

/// <summary>
/// Represents a validated spot submission to a radio.
/// </summary>
public sealed record SpotRequest : IValidatableObject
{
    private static readonly Regex ColorPattern = new("^#?[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$", RegexOptions.Compiled);

    /// <summary>Gets or sets the receive frequency in MHz.</summary>
    [Range(0.001, 10000.0)]
    public double RxFrequency { get; init; }

    /// <summary>Gets or sets the transmit frequency in MHz.</summary>
    [Range(0.001, 10000.0)]
    public double TxFrequency { get; init; }

    /// <summary>Gets or sets the operating mode.</summary>
    [Required]
    [StringLength(32)]
    public string Mode { get; init; } = string.Empty;

    /// <summary>Gets or sets the displayed callsign.</summary>
    [Required]
    [StringLength(32)]
    public string Callsign { get; init; } = string.Empty;

    /// <summary>Gets or sets the foreground color.</summary>
    [StringLength(9)]
    public string Color { get; init; } = "ffff00";

    /// <summary>Gets or sets the background color.</summary>
    [StringLength(9)]
    public string BackgroundColor { get; init; } = string.Empty;

    /// <summary>Gets or sets the origin of the spot.</summary>
    [StringLength(128)]
    public string Source { get; init; } = string.Empty;

    /// <summary>Gets or sets the reporting callsign.</summary>
    [StringLength(32)]
    public string SpotterCallsign { get; init; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp supplied by the spot source.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Gets or sets the number of seconds before the spot expires.</summary>
    [Range(1, 86400)]
    public int LifetimeSeconds { get; init; } = 30;

    /// <summary>Gets or sets the comment displayed with the spot.</summary>
    [StringLength(512)]
    public string Comment { get; init; } = string.Empty;

    /// <summary>Gets or sets the display priority.</summary>
    [Range(1, 5)]
    public int Priority { get; init; } = 5;

    /// <summary>Gets or sets the action the radio performs when the spot is selected.</summary>
    [Required]
    [StringLength(16)]
    public string TriggerAction { get; init; } = "tune";

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Callsign))
        {
            yield return new ValidationResult("Callsign is required.", [nameof(Callsign)]);
        }

        if (string.IsNullOrWhiteSpace(Mode))
        {
            yield return new ValidationResult("Mode is required.", [nameof(Mode)]);
        }

        if (!double.IsFinite(RxFrequency))
        {
            yield return new ValidationResult("Receive frequency must be finite.", [nameof(RxFrequency)]);
        }

        if (!double.IsFinite(TxFrequency))
        {
            yield return new ValidationResult("Transmit frequency must be finite.", [nameof(TxFrequency)]);
        }

        if (!string.IsNullOrEmpty(Color) && !ColorPattern.IsMatch(Color))
        {
            yield return new ValidationResult("Color must be a six- or eight-digit hexadecimal color.", [nameof(Color)]);
        }

        if (!string.IsNullOrEmpty(BackgroundColor) && !ColorPattern.IsMatch(BackgroundColor))
        {
            yield return new ValidationResult("Background color must be a six- or eight-digit hexadecimal color.", [nameof(BackgroundColor)]);
        }

        if (!string.Equals(TriggerAction, "tune", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(TriggerAction, "none", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Trigger action must be 'tune' or 'none'.", [nameof(TriggerAction)]);
        }

        if (Timestamp.Kind == DateTimeKind.Unspecified ||
            Timestamp.ToUniversalTime() < DateTime.UtcNow.AddDays(-366) ||
            Timestamp.ToUniversalTime() > DateTime.UtcNow.AddDays(1))
        {
            yield return new ValidationResult("Timestamp must be UTC and within the supported time window.", [nameof(Timestamp)]);
        }
    }
}
