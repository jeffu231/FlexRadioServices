using FlexRadioServices.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexRadioServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class RadioController: ControllerBase
{
    private readonly ILogger<RadioController> _logger;
    private readonly ActiveState _activeState;
    
    public RadioController(ILogger<RadioController> logger, ActiveState activeState)
    {
        _logger = logger;
        _activeState = activeState;
    }
    
    [Route("radios")]
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Radios()
    {
        return await Task.FromResult(Ok(_activeState.Radios.Values));
    }
    
    [Route("radios/{id}/connect")]
    [HttpPost]
    [MapToApiVersion("1.0")]
    public IActionResult Connect(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest("Invalid Id");
        if (_activeState.Radios.TryGetValue(id.Trim(), out var radio))
        {
            if (_activeState.ActiveRadio != null && _activeState.ActiveRadio.Serial.Equals(id.Trim()))
            {
                return Ok("Already Connected");
            }
            _activeState.ConnectToRadio(radio.Radio);
            
        }
        return Ok("Connected");
    }
    
    [Route("radios/{id}/disconnect")]
    [HttpPost]
    [MapToApiVersion("1.0")]
    public IActionResult Disconnect(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest("Invalid Id");
        if (_activeState.Radios.TryGetValue(id.Trim(), out var radio))
        {
            if (!radio.Radio.Connected)
            {
                return Ok("Not Connected");
            }
            _activeState.DisconnectRadio(radio.Radio);
        }
        
        return Ok("Disconnected");
    }
}