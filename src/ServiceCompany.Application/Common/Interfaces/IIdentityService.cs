using ServiceCompany.Application.Common.Models;
using ServiceCompany.Domain.Common;

namespace ServiceCompany.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<AuthResponse>> LoginAsync(string email, string password);
    Task<Result<AuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<Result> LogoutAsync(string userId);
    Task<List<UserDto>> GetUsersByRoleAsync(string roleName);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<Result> CreateUserWithRoleAsync(string fullName, string email, string password, string role);
    Task<Result> UpdateUserAsync(string userId, string fullName, bool isActive);
    Task<Result> SetUserRoleAsync(string userId, string newRole);
    Task<Result> ResetUserPasswordAsync(string userId, string newPassword);
}
