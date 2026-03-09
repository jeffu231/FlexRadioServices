using System.Net;
using Asp.Versioning;
using FlexRadioServices.Attributes;
using FlexRadioServices.Models;
using FlexRadioServices.Services;
using FlexRadioServices.Utils;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Spot = FlexRadioServices.Models.Spot;

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
    
    /// <summary>
    /// Get a list of all discovered radios.
    /// </summary>
    /// <returns>A List of type <see cref="RadioProxy">Radio</see></returns>
    [HttpGet("radios")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<RadioProxy>), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> Radios()
    {
        return await Task.FromResult(Ok(_flexRadioService.DiscoveredRadios.ToList()));
    }
    
    /// <summary>
    /// Connects a radio.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <returns>Result</returns>
    [HttpPost("radios/{id}/connect")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
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
    
    /// <summary>
    /// Disconnects a radio.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <returns>Result</returns>
    [HttpPost("radios/{id}/disconnect")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
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
            return Ok("Disconnected");
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    /// <summary>
    /// Get all GUI clients for a specific radio.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <returns>A List of type <see cref="RadioClientProxy">RadioClient</see></returns>
    [HttpGet("radios/{id}/clients")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<RadioClientProxy>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public IActionResult Clients(string id)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                var clients = radioProxy.Radio.GuiClients.Select(c => new RadioClientProxy(c));
                return Ok(clients);
            }
            return Problem("Radio not connected", statusCode: 503);
        }

        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    /// <summary>
    /// Get all slices for a specific radio regardless of the client.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <returns>A List of type <see cref="SliceProxy">Slice</see></returns>
    [HttpGet("radios/{id}/slices")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<SliceProxy>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public IActionResult Slices(string id)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                var slices = radioProxy.Radio.SliceList.Select(s => new SliceProxy(s));
                return Ok(slices);
            }
            return Problem("Radio not connected", statusCode: 503);
        }

        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    /// <summary>
    /// Get all slices for a specific client.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <param name="clientId">The client id on the radio.</param>
    /// <returns>A List of type <see cref="SliceProxy">Slice</see></returns>
    [HttpGet("radios/{id}/{clientId}/slices")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<SliceProxy>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public IActionResult Slices(string id, string clientId)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (!radioProxy.Connected)
            {
                var client = radioProxy.GuiClients.FirstOrDefault(c => c.ClientId == clientId);
                if (client == null)
                {
                    return Problem($"Client id {clientId} not found");
                }

                var slices = radioProxy.Radio.SliceList.Where(s => s.ClientHandle == client.ClientHandle)
                    .Select(s => new SliceProxy(s));
                return Ok(slices);
            }
            return Problem("Radio not connected", statusCode: 503);
        }

        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    /// <summary>
    /// Get a specific slice.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <param name="clientId">The client id on the radio.</param>
    /// <param name="letter">The Slice letter identifier within the client.</param>
    /// <returns><see cref="SliceProxy">Slice</see> Information about the Slice requested.</returns>
    [HttpGet("radios/{id}/{clientId}/slices/{letter}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SliceProxy), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public IActionResult Slice(string id, string clientId, [SliceLetter] string letter)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                var client = radioProxy.GuiClients.FirstOrDefault(c => c.ClientId == clientId);
                if (client == null)
                {
                    return Problem($"Client id {clientId} not found");
                }

                var slice = radioProxy.Radio.SliceList.Where(s => s.ClientHandle == client.ClientHandle &&
                                                                  s.Letter.Equals(letter.ToUpper()))
                    .Select(s => new SliceProxy(s)).FirstOrDefault();
                if (slice != null)
                {
                    return Ok(slice);
                }

                return NotFound();
            }
            return Problem($"Radio not connected", statusCode: 503);
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);

    }
    
    /// <summary>
    /// Patches a slice using a JSON Patch document.
    /// </summary>
    /// <param name="id">The radio id.</param>
    /// <param name="clientId">The client id the slice you want to patch is located on.</param>
    /// <param name="letter">The Slice letter identifier within the client.</param>
    /// <param name="slicePatch">JSON Patch document</param>
    /// <returns>Status</returns>
    [HttpPatch("radios/{id}/{clientId}/slices/{letter}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))] 
    [Produces("application/json")]
    public IActionResult PatchSlice(string id, string clientId, [SliceLetter] string letter, 
        [FromBody] JsonPatchDocument<SliceProxy> slicePatch)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                var client =
                    radioProxy.Radio
                        .FindGUIClientByClientID(
                            clientId); //radioProxy.GuiClients.FirstOrDefault(c => c.ClientId == clientId);
                if (client == null)
                {
                    return Problem($"Client id {clientId} not found");
                }

                var s = radioProxy.Radio.FindSliceByLetter(letter, client.ClientHandle);

                if (s != null)
                {
                    var slice = new SliceProxy(s);
                    slicePatch.ApplyToSafely(slice, ModelState);
                    if (!ModelState.IsValid) return ValidationProblem(ModelState);
                    return Ok(slice);
                }
                
                return Problem($"Slice {letter} not found.", statusCode: 404);
            }

            return Problem($"Radio not connected", statusCode: 503);
        }

        return Problem($"Radio {id} not found.", statusCode: 404);

    }

    /// <summary>
    /// Submits a list of spots to the specified radio.
    /// </summary>
    /// <param name="id">The unique identifier of the radio. (Not the client id)</param>
    /// <param name="spots">A List of type <see cref="FlexRadioServices.Models.Spot">Spot</see> to be submitted.</param>
    /// <returns>A response indicating the result of the operation.</returns>
    [HttpPost("radios/{id}/spots")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]
    public IActionResult Spot(string id, [FromBody] List<Spot> spots)
    {
        var radioProxy = _flexRadioService.DiscoveredRadios.FirstOrDefault(r => r.Serial.Equals(id.Trim()));
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                foreach (var spot in spots)
                {
                    //TODO use an automapper library to do this in the future
                    var flexSpot = new Flex.Smoothlake.FlexLib.Spot
                    {
                        Callsign = spot.Callsign,
                        RXFrequency = spot.RxFrequency,
                        TXFrequency = spot.TxFrequency,
                        Mode = spot.Mode,
                        Color = spot.Color,
                        BackgroundColor = spot.BackgroundColor,
                        Source = spot.Source,
                        SpotterCallsign = spot.SpotterCallsign,
                        LifetimeSeconds = spot.LifetimeSeconds,
                        Timestamp = spot.Timestamp,
                        Comment = spot.Comment,
                        Priority = spot.Priority,
                        TriggerAction = spot.TriggerAction
                    };
                    radioProxy.Radio.RequestSpot(flexSpot);
                }

                return Ok();
            }
            
            return Problem($"Radio {id} not connected", statusCode:503);
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpDelete("radios/{id}/spots")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]   
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
            
            return Problem($"Radio {id} not connected", statusCode:503);
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpDelete("radios/{id}/spots/{callsign}/{frequency}")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]  
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
            
            return Problem($"Radio {id} not connected", statusCode:503);
        }
        
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
}