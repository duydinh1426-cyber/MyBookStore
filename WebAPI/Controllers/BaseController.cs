using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Common;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected int AccountId =>
        int.TryParse(User.FindFirstValue("accountId"), out var id) ? id : 0;

    protected int UserId =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var id) ? id : 0;

    protected IActionResult FromResponse<T>(ApiResponse<T> response) =>
        StatusCode(response.StatusCode, response);
}