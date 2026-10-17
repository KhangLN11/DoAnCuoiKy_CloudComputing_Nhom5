
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentFeedback.Controllers;

[ApiController]
[Authorize]
[Route("api/antiforgery")]
public sealed class AntiforgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("token")]
    public IActionResult GetToken()
    {
        Response.Headers.CacheControl = "no-store";

        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new
        {
            requestToken = tokens.RequestToken
        });
    }
}
