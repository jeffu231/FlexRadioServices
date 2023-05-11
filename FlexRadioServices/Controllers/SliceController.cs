using Microsoft.AspNetCore.Mvc;

namespace CoreServices.Controllers;

[ApiController]
[Route("api/frs/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SliceController:ControllerBase
{
    private readonly ILogger<SliceController> _logger;
    
    public SliceController(ILogger<SliceController> logger)
    {
        _logger = logger;
    }
}