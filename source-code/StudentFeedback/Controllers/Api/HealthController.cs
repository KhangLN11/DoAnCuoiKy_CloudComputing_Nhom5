
using Microsoft.AspNetCore.Mvc;

namespace StudentFeedback.Controllers.Api;

[ApiController]
[Route("api/v1/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(HealthResponse),
        StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Application = "StudentFeedback",
            ApiVersion = "v1",
            TimestampUtc = DateTime.UtcNow
        });
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;

    public string Application { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }
}
