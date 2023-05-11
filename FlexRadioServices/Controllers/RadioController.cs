using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class RadioController: ControllerBase
{
    private readonly ILogger<RadioController> _logger;
    
    public RadioController(ILogger<RadioController> logger)
    {
        _logger = logger;
    }

    [Route("{id}")]
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Get(string id, [FromQuery] string param = null)
    {
        
    }
}