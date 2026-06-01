using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Domain.Common;
using ServiceCompany.Infrastructure.Common;
using ServiceCompany.Infrastructure.Identity;

namespace ServiceCompany.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public IdentityService(UserManager<ApplicationUser> userManager, ITokenService tokenService, Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return result.ToApplicationResult();
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(string roleName)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto(user.Id, user.UserName!, user.FullName, user.Email!, roles.ToList(), user.IsActive));
        }

        return userDtos;
    }

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
            return Result.Failure<AuthResponse>("Invalid credentials");

        var result = await _userManager.CheckPasswordAsync(user, password);
        if (!result)
            return Result.Failure<AuthResponse>("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _userManager.UpdateAsync(user);

        return Result.Success(new AuthResponse(
            accessToken,
            refreshToken,
            new UserDto(user.Id, user.UserName ?? "", user.FullName, user.Email ?? "", roles.ToList(), user.IsActive)
        ));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Result.Failure<AuthResponse>("Invalid token");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result.Failure<AuthResponse>("Invalid refresh token");

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _userManager.UpdateAsync(user);

        return Result.Success(new AuthResponse(
            newAccessToken,
            newRefreshToken,
            new UserDto(user.Id, user.UserName ?? "", user.FullName, user.Email ?? "", roles.ToList(), user.IsActive)
        ));
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure("Пользователь не найден.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure("User not found");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(user.Id, user.UserName!, user.FullName, user.Email!, roles.ToList(), user.IsActive));
        }
        return result;
    }

    public async Task<Result> CreateUserWithRoleAsync(string fullName, string email, string password, string role)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            return Result.Failure("Пользователь с таким email уже существует.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            return Result.Failure(string.Join("; ", roleResult.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> UpdateUserAsync(string userId, string fullName, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure("Пользователь не найден.");

        user.FullName = fullName;
        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> SetUserRoleAsync(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure("Пользователь не найден.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var result = await _userManager.AddToRoleAsync(user, newRole);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> ResetUserPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure("Пользователь не найден.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
