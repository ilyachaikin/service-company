using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;

namespace ServiceCompany.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AuthController(IIdentityService identityService, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _identityService.LoginAsync(request.Email, request.Password);

        if (result.IsSuccess)
        {
            var authResponse = result.Value!;
            await _auditService.LogAsync(authResponse.User.Id, "Login", "User", authResponse.User.Id);
            return Ok(authResponse);
        }

        await _auditService.LogAsync("Anonymous", "FailedLogin", "User", request.Email);
        return BadRequest(result.Error);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRefreshRequest request)
    {
        var result = await _identityService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _identityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _identityService.LogoutAsync(userId);
        if (result.IsSuccess)
        {
            await _auditService.LogAsync(userId, "Logout", "User", userId);
            return Ok();
        }

        return BadRequest(result.Error);
    }
}
