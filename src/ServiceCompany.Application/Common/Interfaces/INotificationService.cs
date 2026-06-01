namespace ServiceCompany.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, bool sendEmail = false, CancellationToken ct = default);
}
