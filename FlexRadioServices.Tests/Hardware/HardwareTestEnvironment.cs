namespace FlexRadioServices.Tests.Hardware;

/// <summary>
/// Controls whether tests that may access a physical radio are permitted to run.
/// </summary>
internal static class HardwareTestEnvironment
{
    internal const string EnableVariableName = "FLEXRADIOSERVICES_RUN_HARDWARE_TESTS";

    internal static bool IsEnabled => string.Equals(
        Environment.GetEnvironmentVariable(EnableVariableName),
        "1",
        StringComparison.Ordinal);
}
