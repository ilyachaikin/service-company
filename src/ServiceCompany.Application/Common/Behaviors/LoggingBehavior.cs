using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceCompany.Application.Common.Interfaces;

namespace ServiceCompany.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId      = _currentUserService.UserId ?? "Аноним";

        _logger.LogInformation("→ Запрос: {Name} | Пользователь: {UserId}", requestName, userId);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation(
                "← Выполнен: {Name} | {Ms} мс | Пользователь: {UserId}",
                requestName, sw.ElapsedMilliseconds, userId);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "✗ Ошибка: {Name} | {Ms} мс | Пользователь: {UserId}",
                requestName, sw.ElapsedMilliseconds, userId);
            throw;
        }
    }
}
