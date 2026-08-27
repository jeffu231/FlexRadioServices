using System.Net;
using System.Reflection;
using System.Collections.Immutable;
using Asp.Versioning;
using FlexRadioServices.Models.Api;
using FlexRadioServices.Models.Configuration;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public sealed class ConfigurationController(
    ICatPortConfigurationProvider catPortConfigurationProvider,
    IOptions<MqttBrokerSettings> mqttBrokerSettings,
    IOptions<RadioSettings> radioSettings) : ControllerBase
{
    /// <summary>
    /// Get the version of the application
    /// </summary>
    /// <returns>Application Version</returns>
    [HttpGet("version")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(ApplicationVersionResponse), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public ActionResult<ApplicationVersionResponse> GetVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        return Ok(new ApplicationVersionResponse(version));
    }

    /// <summary>
    /// Retrieves the configured and effective CAT port settings.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the CAT port settings.</returns>
    [HttpGet("catport/settings")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(CatPortSettingsResponse), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public ActionResult<CatPortSettingsResponse> GetCatPortSettings()
    {
        var profiles = catPortConfigurationProvider.GetConfiguredProfiles();
        var activeBindingsByProfile = catPortConfigurationProvider.GetActiveBindings()
            .GroupBy(binding => binding.ProfileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);
        var configured = new ConfiguredCatPortSettingsResponse(
            profiles.Select(CreateConfiguredProfile).ToImmutableArray(),
            catPortConfigurationProvider.GetConfiguredClients()
                .Select(client => new ConfiguredCatClientResponse(
                    client.ClientId,
                    client.ClientFriendlyName,
                    client.Enabled,
                    client.ProfileName))
                .ToImmutableArray());
        var effectiveProfiles = profiles.Select(profile => CreateEffectiveProfile(profile, activeBindingsByProfile))
            .ToImmutableArray();

        return Ok(new CatPortSettingsResponse(configured, effectiveProfiles));
    }

    /// <summary>
    /// Retrieves the MQTT broker settings configuration.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the MQTT settings.</returns>
    [HttpGet("mqtt/settings")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(MqttSettingsResponse), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetMqttBrokerSettings()
    {
        var settings = mqttBrokerSettings.Value;
        var response = new MqttSettingsResponse(
            settings.BrokerHost,
            settings.BrokerPort,
            settings.ClientId,
            settings.ClientUser,
            settings.RootTopic,
            !string.IsNullOrWhiteSpace(settings.ClientPassword));

        return await Task.FromResult(Ok(response));
    }

    /// <summary>
    /// Retrieves the radio settings configuration.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the radio settings.</returns>
    [HttpGet("radio/settings")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(RadioSettings), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetRadioSettings()
    {
        return await Task.FromResult(Ok(radioSettings.Value));
    }

    private static ConfiguredCatPortProfileResponse CreateConfiguredProfile(CatPortProfileSettings profile) => new(
        profile.ProfileName,
        profile.PortSettings.Select(CreateConfiguredPortSettings).ToImmutableArray());

    private static ConfiguredPortSettingsResponse CreateConfiguredPortSettings(PortSettings portSettings) => new(
        portSettings.PortFriendlyName,
        portSettings.Protocol,
        portSettings.PortNumber,
        portSettings.PortSliceType,
        portSettings.VfoASliceLetter,
        portSettings.VfoBSliceLetter,
        portSettings.AutoSwitchTxSlice);

    private static EffectiveCatPortProfileResponse CreateEffectiveProfile(
        CatPortProfileSettings profile,
        IReadOnlyDictionary<string, ImmutableArray<ResolvedCatPortBinding>> activeBindingsByProfile)
    {
        if (!activeBindingsByProfile.TryGetValue(profile.ProfileName, out var activeBindings))
        {
            return new EffectiveCatPortProfileResponse(profile.ProfileName, false, null, []);
        }

        var activeBinding = activeBindings[0];
        return new EffectiveCatPortProfileResponse(
            profile.ProfileName,
            true,
            new ActiveCatClientResponse(activeBinding.ClientId, activeBinding.ClientFriendlyName),
            activeBindings.Select(CreateEffectiveListener).ToImmutableArray());
    }

    private static EffectiveCatPortListenerResponse CreateEffectiveListener(ResolvedCatPortBinding binding) => new(
        binding.PortSettings.PortFriendlyName,
        binding.PortSettings.Protocol,
        binding.PortSettings.PortNumber,
        binding.PortSettings.PortSliceType,
        binding.PortSettings.VfoASliceLetter,
        binding.PortSettings.VfoBSliceLetter,
        binding.PortSettings.AutoSwitchTxSlice);
}
