namespace ServiceCompany.Application.Common.Models;

public record UserDto(string Id, string UserName, string FullName, string Email, List<string> Roles, bool IsActive = true);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User);

public record LoginRequest(
    string Email,
    string Password);

public record TokenRefreshRequest(
    string AccessToken,
    string RefreshToken);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record CreateUserRequest(string FullName, string Email, string Password, string Role);
public record UpdateUserRequest(string FullName, bool IsActive);
public record SetUserRoleRequest(string Role);
public record ResetPasswordRequest(string NewPassword);
