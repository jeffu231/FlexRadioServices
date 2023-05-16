using FlexRadioServices.Attributes;
using FlexRadioServices.Models;
using FlexRadioServices.Utils;
using Microsoft.AspNetCore.JsonPatch;
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
    
    [HttpGet("radios")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Radios()
    {
        return await Task.FromResult(Ok(_activeState.Radios.Values));
    }
    
    [HttpPost("radios/{id}/connect")]
    [MapToApiVersion("1.0")]
    public IActionResult Connect(string id)
    {
        if (_activeState.Radios.TryGetValue(id.Trim(), out var radio))
        {
            if (radio.Radio.Connected)
            {
                return Ok("Already Connected");
            }
            _activeState.ConnectToRadio(radio.Radio);
            return Ok("Connected");
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpPost("radios/{id}/disconnect")]
    [MapToApiVersion("1.0")]
    public IActionResult Disconnect(string id)
    {
        if (_activeState.Radios.TryGetValue(id.Trim(), out var radio))
        {
            if (!radio.Radio.Connected)
            {
                return Ok("Not Connected");
            }
            _activeState.DisconnectRadio(radio.Radio);
            return Ok("Disconnected");
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpGet("radios/{id}/slices")]
    [MapToApiVersion("1.0")]
    public IActionResult Slices(string id)
    {
        if (_activeState.Radios.TryGetValue(id, out var radioProxy))
        {
            var slices = radioProxy.Radio.SliceList.Select(s => new SliceProxy(s));
            return Ok(slices);
        }

        return Problem($"Radio {id} not found.", statusCode: 404);
    }

    [HttpPatch("radios/{id}/slices/{letter}")]
    public IActionResult PatchSlice(string id, [SliceLetter] string letter, 
        [FromBody] JsonPatchDocument<SliceProxy> slicePatch)
    {
        if (_activeState.Radios.TryGetValue(id, out var radioProxy))
        {
            var slice = radioProxy.Radio.SliceList.Where(s => s.Letter.Equals(letter.ToUpper()))
                .Select(s => new SliceProxy(s)).FirstOrDefault();
            if (slice != null)
            {
                slicePatch.ApplyToSafely(slice, ModelState);
                if (!ModelState.IsValid) return ValidationProblem(ModelState);
                return Ok(slice);
            }
            
            return Problem($"Slice {letter} not found.", statusCode: 404);
        }

        return Problem("Radio {id} not found", statusCode:404);

    }
    
    [HttpGet("radios/{id}/slices/{letter}")]
    public IActionResult Slice(string id, [SliceLetter] string letter)
    {
        if (_activeState.Radios.TryGetValue(id, out var radioProxy))
        {
            var slice = radioProxy.Radio.SliceList.Where(s => s.Letter.Equals(letter.ToUpper()))
                .Select(s => new SliceProxy(s)).FirstOrDefault();
            if (slice != null)
            {
                return Ok(slice);
            }

            return Problem($"Slice {letter} not found.", statusCode:404);
        }

        return Problem("Radio {id} not found", statusCode:404);

    }
}