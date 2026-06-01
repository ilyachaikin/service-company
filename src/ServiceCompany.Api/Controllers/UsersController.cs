using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;

namespace ServiceCompany.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsersController(IIdentityService identityService)
        => _identityService = identityService;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _identityService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _identityService.CreateUserWithRoleAsync(
            request.FullName, request.Email, request.Password, request.Role);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var result = await _identityService.UpdateUserAsync(id, request.FullName, request.IsActive);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> SetRole(string id, [FromBody] SetUserRoleRequest request)
    {
        var result = await _identityService.SetUserRoleAsync(id, request.Role);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
    {
        var result = await _identityService.ResetUserPasswordAsync(id, request.NewPassword);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
