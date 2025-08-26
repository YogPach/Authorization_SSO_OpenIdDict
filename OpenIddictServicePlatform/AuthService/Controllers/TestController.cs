using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("read")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult ReadData()
    {
        // Extract scopes from token
        var scopes = User.Claims
            .Where(c => c.Type == "oi_scp")
            .Select(c => c.Value)
            .ToList();

        if (!scopes.Contains("api.read"))
            return Forbid(); // 403 if missing scope

        return Ok(new { message = "Token is valid and scope is allowed!" });
    }
}
