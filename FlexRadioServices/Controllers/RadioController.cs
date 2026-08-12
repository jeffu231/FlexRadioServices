using System.Net;
using Asp.Versioning;
using FlexRadioServices.Attributes;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Api;
using FlexRadioServices.Models.Radio;
using FlexRadioServices.Services;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Spot = FlexRadioServices.Models.Spot;

namespace FlexRadioServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public sealed class RadioController(ILogger<RadioController> logger, IFlexRadioService flexRadioService,
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
    public ActionResult<IEnumerable<RadioSnapshot>> Radios() => Ok(_flexRadioService.GetDiscoveredRadios());
    
    /// <summary>
    /// Connects a radio.
    /// </summary>
    /// <param name="id">The id of the radio.</param>
    /// <returns>Result</returns>
    [HttpPost("radios/{id}/connect")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(OperationResultResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public ActionResult<OperationResultResponse> Connect(string id)
    {
        var radio = _flexRadioService.GetDiscoveredRadios().FirstOrDefault(r => r.Serial.Equals(id.Trim(), StringComparison.Ordinal));
        if (radio != null)
        {
            if (radio.Connected)
            {
                return Ok(new OperationResultResponse("Already connected"));
            }
            _flexRadioService.ConnectToRadio(radio.Serial);
            return Ok(new OperationResultResponse("Connected"));
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
    [ProducesResponseType(typeof(OperationResultResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(ProblemDetails))]
    [Produces("application/json")]
    public ActionResult<OperationResultResponse> Disconnect(string id)
    {
        var radio = _flexRadioService.GetDiscoveredRadios().FirstOrDefault(r => r.Serial.Equals(id.Trim(), StringComparison.Ordinal));
        if (radio != null)
        {
            if (!radio.Connected)
            {
                return Ok(new OperationResultResponse("Already disconnected"));
            }
            _flexRadioService.DisconnectRadio(radio.Serial);
            return Ok(new OperationResultResponse("Disconnected"));
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
    [ProducesResponseType((int)HttpStatusCode.Conflict, Type = typeof(ProblemDetails))]
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
            
            _logger.LogWarning("Radio {Id} is not connected", id);
            return DisconnectedRadioProblem(id);
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
    [ProducesResponseType((int)HttpStatusCode.Conflict, Type = typeof(ProblemDetails))]
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
            
            _logger.LogWarning("Radio {Id} is not connected", id);
            return DisconnectedRadioProblem(id);
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
    [ProducesResponseType((int)HttpStatusCode.Conflict, Type = typeof(ProblemDetails))]
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
                    return Problem($"Client id {clientId} not found.", statusCode: 404);
                }

                var slices = radioProxy.Radio.SliceList.Where(s => s.ClientHandle == client.ClientHandle)
                    .Select(s => new SliceProxy(s));
                return Ok(slices);
            }
            
            _logger.LogWarning("Radio {Id} is not connected", id);
            return DisconnectedRadioProblem(id);
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
    [ProducesResponseType((int)HttpStatusCode.Conflict, Type = typeof(ProblemDetails))]
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
                    return Problem($"Client id {clientId} not found.", statusCode: 404);
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
            
            _logger.LogWarning("Radio {Id} is not connected", id);
            return DisconnectedRadioProblem(id);
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
    [ProducesResponseType((int)HttpStatusCode.Conflict, Type = typeof(ProblemDetails))]
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
                    return Problem($"Client id {clientId} not found.", statusCode: 404);
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

            _logger.LogWarning("Radio {Id} is not connected", id);
            return DisconnectedRadioProblem(id);
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
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(ProblemDetails))]
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
    [MapToApiVersion("1.0")]
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
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(ProblemDetails))]
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

    /// <summary>
    /// Submits a validated list of spots to a connected radio.
    /// </summary>
    /// <param name="id">The unique identifier of the radio.</param>
    /// <param name="spots">The spots to submit.</param>
    /// <returns>The result of the submission.</returns>
    [HttpPost("radios/{id}/spots")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(OperationResultResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [Produces("application/json")]
    public ActionResult<OperationResultResponse> SubmitSpots(string id, [FromBody] List<SpotRequest>? spots)
    {
        SpotRequestValidator.ValidateBatch(spots, ModelState);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var radioProxy = GetRadioHandle(id);
        if (radioProxy is null)
        {
            _logger.LogWarning("Radio {Id} was not found", id);
            return Problem($"Radio {id} not found.", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!radioProxy.Connected)
        {
            _logger.LogWarning("Radio {Id} is not connected", id);
            return DisconnectedRadioProblem(id);
        }

        foreach (var spot in spots!)
        {
            radioProxy.Radio.RequestSpot(CreateFlexSpot(spot));
        }

        return Ok(new OperationResultResponse("Spots submitted"));
    }

    /// <summary>
    /// Removes all spots from a connected radio.
    /// </summary>
    /// <param name="id">The unique identifier of the radio.</param>
    /// <returns>The result of the operation.</returns>
    [HttpDelete("radios/{id}/spots")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(OperationResultResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [Produces("application/json")]
    public ActionResult<OperationResultResponse> ClearSpots(string id)
    {
        var radioProxy = GetRadioHandle(id);
        if (radioProxy is null)
        {
            return Problem($"Radio {id} not found.", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!radioProxy.Connected)
        {
            return DisconnectedRadioProblem(id);
        }

        radioProxy.Radio.ClearAllSpots();
        return Ok(new OperationResultResponse("Spots cleared"));
    }

    /// <summary>
    /// Removes a spot from a connected radio.
    /// </summary>
    /// <param name="id">The unique identifier of the radio.</param>
    /// <param name="callsign">The callsign displayed by the spot.</param>
    /// <param name="frequency">The receive frequency in MHz.</param>
    /// <returns>The result of the operation.</returns>
    [HttpDelete("radios/{id}/spots/{callsign}/{frequency}")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(OperationResultResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [Produces("application/json")]
    public ActionResult<OperationResultResponse> RemoveSpot(string id, [FromRoute] string callsign, double frequency)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            ModelState.TryAddModelError(nameof(callsign), "Callsign is required.");
        }

        if (!double.IsFinite(frequency) || frequency is <= 0 or > 10000)
        {
            ModelState.TryAddModelError(nameof(frequency), "Frequency must be a finite value between 0.001 and 10000 MHz.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var radioProxy = GetRadioHandle(id);
        if (radioProxy is null)
        {
            return Problem($"Radio {id} not found.", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!radioProxy.Connected)
        {
            return DisconnectedRadioProblem(id);
        }

        radioProxy.Radio.RemoveSpot(callsign, frequency);
        return Ok(new OperationResultResponse("Spot removed"));
    }

    private ObjectResult DisconnectedRadioProblem(string id) => Problem(
        detail: $"Radio {id} is not connected.",
        statusCode: (int)HttpStatusCode.Conflict,
        title: "Radio is disconnected",
        type: "https://flexradioservices.dev/problems/radio-disconnected");

    private static Flex.Smoothlake.FlexLib.Spot CreateFlexSpot(SpotRequest spot) => new()
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

    private RadioProxy? GetRadioHandle(string id) =>
        (_flexRadioService as FlexRadioService)?.GetRadioHandle(id.Trim());
}
