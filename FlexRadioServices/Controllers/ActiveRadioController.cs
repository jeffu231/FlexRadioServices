using FlexRadioServices.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexRadioServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ActiveRadioController:ControllerBase
{
    private readonly ILogger<RadioController> _logger;
    private readonly ActiveState _activeState;
    
    public ActiveRadioController(ILogger<RadioController> logger, ActiveState activeState)
    {
        _logger = logger;
        _activeState = activeState;
    }
    
    [Route("")]
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
    
    [Route("slices")]
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult Slices()
    {
        if (_activeState.ActiveRadio != null)
        {
            var slices = _activeState.ActiveRadio.SliceList.Select(s => new SliceProxy(s));
            return Ok(slices);
        }

        return Ok(Enumerable.Empty<SliceProxy>());
    }
    
}