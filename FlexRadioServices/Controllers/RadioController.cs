using System.Net;
using Asp.Versioning;
using FlexRadioServices.Attributes;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Radio;
using FlexRadioServices.Services;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Spot = FlexRadioServices.Models.Spot;

namespace FlexRadioServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class RadioController(ILogger<RadioController> logger, IFlexRadioService flexRadioService,
    ISliceCommandService sliceCommandService) : ControllerBase
{
    private readonly ILogger<RadioController> _logger = logger;
    private readonly IFlexRadioService _flexRadioService = flexRadioService;
    
    /// <summary>
    /// Get a list of all discovered radios.
    /// </summary>
    /// <returns>A copied list of discovered radio state.</returns>
    [HttpGet("radios")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<RadioSnapshot>), (int)HttpStatusCode.OK)]
    [Produces("application/json")]
    public async Task<IActionResult> Radios()
    {
        return await Task.FromResult(Ok(_flexRadioService.GetDiscoveredRadios()));
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
        var radio = _flexRadioService.GetDiscoveredRadios().FirstOrDefault(r => r.Serial.Equals(id.Trim(), StringComparison.Ordinal));
        if (radio != null)
        {
            if (radio.Connected)
            {
                return Ok("Already Connected");
            }
            _flexRadioService.ConnectToRadio(radio.Serial);
            return Ok("Connected");
        }
        
        _logger.LogError("Radio {Id} not found", id);
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
        var radio = _flexRadioService.GetDiscoveredRadios().FirstOrDefault(r => r.Serial.Equals(id.Trim(), StringComparison.Ordinal));
        if (radio != null)
        {
            if (!radio.Connected)
            {
                return Ok("Already disconnected");
            }
            _flexRadioService.DisconnectRadio(radio.Serial);
            return Ok("Disconnected");
        }
        
        _logger.LogError("Radio {Id} not found", id);
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    /// <summary>
    /// Get all GUI clients for a specific radio.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <returns>A copied list of GUI client state.</returns>
    [HttpGet("radios/{id}/clients")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<RadioClientSnapshot>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public IActionResult Clients(string id)
    {
        var radio = _flexRadioService.GetDiscoveredRadios()
            .FirstOrDefault(candidate => candidate.Serial.Equals(id.Trim(), StringComparison.Ordinal));
        if (radio != null)
        {
            if (radio.Connected)
            {
                return Ok(_flexRadioService.GetRadioClients(radio.Serial));
            }
            
            _logger.LogError("Radio {Id} not found", id);
            return Problem($"Radio {id} not connected", statusCode: 503);
        }

        _logger.LogError("Radio {Id} not found", id);
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
        var radioProxy = GetRadioHandle(id);
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                var slices = radioProxy.Radio.SliceList.Select(s => new SliceProxy(s));
                return Ok(slices);
            }
            
            _logger.LogError("Radio {Id} not connected", id);
            return Problem($"Radio {id} not connected", statusCode: 503);
        }

        _logger.LogError("Radio {Id} not found", id);
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
        var radioProxy = GetRadioHandle(id);
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
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
            
            _logger.LogError("Radio {Id} not connected", id);
            return Problem("Radio not connected", statusCode: 503);
        }

        _logger.LogError("Radio {Id} not found", id);
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
        var radioProxy = GetRadioHandle(id);
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
            
            _logger.LogError("Radio {Id} not connected", id);
            return Problem($"Radio not connected", statusCode: 503);
        }
        
        _logger.LogError("Radio {Id} not found", id);
        return Problem($"Radio {id} not found.", statusCode: 404);

    }
    
    /// <summary>
    /// Patches a slice using a JSON Patch document.
    /// </summary>
    /// <param name="id">The radio id.</param>
    /// <param name="clientId">The client id the slice you want to patch is located on.</param>
    /// <param name="letter">The Slice letter identifier within the client.</param>
    /// <param name="slicePatch">JSON Patch document</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>Status</returns>
    [HttpPatch("radios/{id}/{clientId}/slices/{letter}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SlicePatchState), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))] 
    [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public async Task<IActionResult> PatchSlice(string id, string clientId, [SliceLetter] string letter,
        [FromBody] JsonPatchDocument<SlicePatchState> slicePatch, CancellationToken cancellationToken)
    {
        var radioProxy = GetRadioHandle(id);
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
                    SlicePatchValidator.ValidateOperations(slicePatch, ModelState);
                    if (!ModelState.IsValid) return ValidationProblem(ModelState);

                    var original = SlicePatchState.FromSlice(s);
                    var desired = new SlicePatchState
                    {
                        Freq = original.Freq, Mode = original.Mode, IsTransmitSlice = original.IsTransmitSlice,
                        Active = original.Active, NROn = original.NROn, NBOn = original.NBOn, WNBOn = original.WNBOn,
                        ANFOn = original.ANFOn, APFOn = original.APFOn, NrLevel = original.NrLevel, NbLevel = original.NbLevel,
                        WnbLevel = original.WnbLevel, AnfLevel = original.AnfLevel, ApfLevel = original.ApfLevel, Mute = original.Mute,
                        AudioGain = original.AudioGain, AudioPan = original.AudioPan, Lock = original.Lock
                    };
                    slicePatch.ApplyTo(desired, error => ModelState.TryAddModelError(error.Operation.path ?? string.Empty, error.ErrorMessage));
                    if (!ModelState.IsValid) return ValidationProblem(ModelState);

                    SlicePatchValidator.ValidateState(desired, s.ModeList, ModelState);
                    if (!ModelState.IsValid) return ValidationProblem(ModelState);

                    try
                    {
                        var committed = await sliceCommandService.ApplyAsync(
                            new SliceIdentity(id.Trim(), client.ClientHandle, letter), new SliceChangeSet(original, desired), cancellationToken);
                        return Ok(committed);
                    }
                    catch (InvalidOperationException)
                    {
                        return NotFound();
                    }
                    catch (SliceCommandException exception)
                    {
                        _logger.LogError(exception, "Failed to commit patch for slice {Letter}", letter);
                        return Problem("The radio rejected a change and its state may have changed.", statusCode: 500);
                    }
                }
                
                _logger.LogError("Slice {Letter} not found", letter);
                return Problem($"Slice {letter} not found.", statusCode: 404);
            }

            _logger.LogError("Radio {Id} not connected", id);
            _logger.LogError("Radio {Id} not connected", id);
            return Problem($"Radio not connected", statusCode: 503);
        }

        _logger.LogError("Radio {Id} not found", id);
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
        var radioProxy = GetRadioHandle(id);
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
            
            _logger.LogError("Radio {Id} not connected", id);
            return Problem($"Radio {id} not connected", statusCode:503);
        }
        
        _logger.LogError("Radio {Id} not found", id);
        return Problem($"Radio {id} not found.", statusCode: 404);
    }
    
    [HttpDelete("radios/{id}/spots")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(ProblemDetails))]   
    public IActionResult Spot(string id)
    {
        var radioProxy = GetRadioHandle(id);
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                radioProxy.Radio.ClearAllSpots();

                return Ok();
            }
            
            _logger.LogError("Radio {Id} not connected", id);
            return Problem($"Radio {id} not connected", statusCode:503);
        }
        
        _logger.LogError("Radio {Id} not found", id);
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
        var radioProxy = GetRadioHandle(id);
        if (radioProxy != null)
        {
            if (radioProxy.Connected)
            {
                radioProxy.Radio.RemoveSpot(callsign, frequency);
                return Ok();
            }
            
            _logger.LogError("Radio {Id} not connected", id);
            return Problem($"Radio {id} not connected", statusCode:503);
        }
        
        _logger.LogError("Radio {Id} not found", id);
        return Problem($"Radio {id} not found.", statusCode: 404);
    }

    private RadioProxy? GetRadioHandle(string id) =>
        (_flexRadioService as FlexRadioService)?.GetRadioHandle(id.Trim());
}
