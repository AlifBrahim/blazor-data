using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Extensions;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var id = User.GetUserId();
        var email = User.Identity?.Name ?? string.Empty;
        var roles = User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToArray();

        return Ok(new UserInfoResponse(id, email, roles));
    }

    private sealed record UserInfoResponse(string Id, string Email, string[] Roles);
}
