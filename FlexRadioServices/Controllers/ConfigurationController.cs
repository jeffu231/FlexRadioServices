using System.Net;
using System.Reflection;
using Asp.Versioning;
using FlexRadioServices.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ConfigurationController:ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IOptions<CatPortSettings> _catPortSettings;
    private readonly IOptions<MqttBrokerSettings> _mqttBrokerSettings;
    private readonly IOptions<RadioSettings> _radioSettings;
    
    public ConfigurationController(ILogger<ConfigurationController> logger, IOptions<CatPortSettings> catPortSettings, 
        IOptions<MqttBrokerSettings> mqttBrokerSettings, IOptions<RadioSettings> radioSettings)
    {
        _logger = logger;
        _catPortSettings = catPortSettings;
        _mqttBrokerSettings = mqttBrokerSettings;
        _radioSettings = radioSettings;
    }
    
    /// <summary>
    /// Get the version of the application
    /// </summary>
    /// <returns>Application Version</returns>
    [HttpGet("version")]
    [MapToApiVersion("1.0")]
    public IActionResult GetVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        return Ok(new { ApplicationVersion = version });
    }

    /// <summary>
    /// Retrieves the CAT port settings configuration.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the CAT port settings.</returns>
    [HttpGet("catport/settings")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(CatPortSettings), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetCatPortSettings()
    {
        return await Task.FromResult(Ok(_catPortSettings.Value));
    }

    /// <summary>
    /// Retrieves the MQTT broker settings configuration.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the MQTT settings.</returns>
    [HttpGet("mqtt/settings")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(MqttBrokerSettings), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetMqttBrokerSettings()
    {
        return await Task.FromResult(Ok(_mqttBrokerSettings.Value));
    }

    /// <summary>
    /// Retrieves the radio settings configuration.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the radio settings.</returns>
    [HttpGet("radio/settings")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(RadioSettings), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetRadioSettings()
    {
        return await Task.FromResult(Ok(_radioSettings.Value));   
    }
}