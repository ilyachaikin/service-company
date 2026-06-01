using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ServiceCompany.Application.Common.Interfaces;

namespace ServiceCompany.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "1025");

        using var client = new SmtpClient(smtpHost, smtpPort);

        var mailMessage = new MailMessage
        {
            From = new MailAddress("noreply@servicecompany.com", "ServiceCompany"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage, ct);
    }
}
