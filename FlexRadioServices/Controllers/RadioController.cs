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

    // [Route("{id}")]
    // [HttpGet]
    // [MapToApiVersion("1.0")]
    // public async Task<IActionResult> Get(string id, [FromQuery] string param = null)
    // {
    //     
    // }
    
    [Route("radios")]
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Radios()
    {
        return await Task.FromResult(Ok(_activeState.Radios.Values));
    }
    
    [Route("activeradio")]
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult ActiveRadio()
    {
        if (_activeState.ActiveRadio != null && _activeState.Radios.TryGetValue(_activeState.ActiveRadio.Serial, out var radio))
        {
            return Ok(radio);
        }

        return NotFound("No Active Radio");
    }
    
    [Route("clients")]
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult Clients()
    {
        return Ok(_activeState.Clients);
    }
    
    [Route("connect/{id}")]
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
    
    [Route("disconnect")]
    [HttpPost]
    [MapToApiVersion("1.0")]
    public IActionResult Disconnect()
    {
        if (_activeState.ActiveRadio == null)
        {
            return Ok("Not connected");
        }
       
        _activeState.DisconnectRadio(_activeState.ActiveRadio);
        
        return Ok("Disconnected");
    }
}