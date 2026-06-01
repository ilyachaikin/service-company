using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Infrastructure.Identity;

namespace ServiceCompany.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeService _dateTimeService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IDateTimeService dateTimeService,
        UserManager<ApplicationUser> userManager,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _dateTimeService = dateTimeService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SendNotificationAsync(
        string userId, string title, string message,
        bool sendEmail = false, CancellationToken ct = default)
    {

        var notification = new Notification
        {
            Title   = title,
            Message = message,
            UserId  = userId,
            IsRead  = false,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);

        if (sendEmail)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user?.Email is { Length: > 0 } email)
                    await _emailSender.SendAsync(email, title, message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить email уведомление пользователю {UserId}", userId);
            }
        }
    }
}
