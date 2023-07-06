using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Attributes;
using FlexRadioServices.Models;
using FlexRadioServices.Services;
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
    private readonly IFlexRadioService _flexRadioService;
    
    public RadioController(ILogger<RadioController> logger, IFlexRadioService flexRadioService)
    {
        _logger = logger;
        _flexRadioService = flexRadioService;
    }
    
    [HttpGet("radios")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Radios()
    {
        return await Task.FromResult(Ok(_flexRadioService.DiscoveredRadios.ToList()));
    }
    
    [HttpPost("radios/{id}/connect")]
    [MapToApiVersion("1.0")]
    public IActionResult Connect(string id)
    {
        var radio = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radio != null)
        {
            if (radio.Radio.Connected)
            {
                return Ok("Already Connected");
            }
            _flexRadioService.ConnectToRadio(radio);
            return Ok("Connected");
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpPost("radios/{id}/disconnect")]
    [MapToApiVersion("1.0")]
    public IActionResult Disconnect(string id)
    {
        var radio = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radio != null)
        {
            if (!radio.Radio.Connected)
            {
                return Ok("Already disconnected");
            }
            _flexRadioService.DisconnectRadio(radio);
            return Ok("Connected");
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpGet("radios/{id}/slices")]
    [MapToApiVersion("1.0")]
    public IActionResult Slices(string id)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
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
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
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
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
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

    [HttpPost("radios/{id}/spots")]
    public IActionResult Spot(string id, [FromBody] List<Spot> spots)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                foreach (var spot in spots)
                {
                    radioProxy.Radio.RequestSpot(spot);
                }

                return Ok();
            }
            
            return Problem("Radio {id} not connected", statusCode:400);
        }
        
        return Problem("Radio {id} not found", statusCode:404);
    }
    
    [HttpDelete("radios/{id}/spots")]
    public IActionResult Spot(string id)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                radioProxy.Radio.ClearAllSpots();

                return Ok();
            }
            
            return Problem("Radio {id} not connected", statusCode:400);
        }
        
        return Problem("Radio {id} not found", statusCode:404);
    }
    
    [HttpDelete("radios/{id}/spots/{callsign}/{frequency}")]
    public IActionResult Spot(string id, string callsign, double frequency)
    {
        if (string.IsNullOrEmpty(callsign))
        {
            return Problem("callsign is null or empty", statusCode: 400);
        }
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                radioProxy.Radio.RemoveSpot(callsign, frequency);

                return Ok();
            }
            
            return Problem("Radio {id} not connected", statusCode:400);
        }
        
        return Problem("Radio {id} not found", statusCode:404);
    }
}