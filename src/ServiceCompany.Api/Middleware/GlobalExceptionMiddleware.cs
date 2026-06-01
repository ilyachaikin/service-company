using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Domain.Common;

namespace ServiceCompany.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Необработанное исключение при обработке запроса {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode code;
        object result;

        switch (exception)
        {
            case FluentValidation.ValidationException validationException:
                code = HttpStatusCode.BadRequest;
                result = new ValidationProblemDetails(
                    validationException.Errors
                        .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                        .ToDictionary(g => g.Key, g => g.ToArray()))
                {
                    Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title  = "Ошибка валидации данных.",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = "Проверьте поле 'errors' для получения подробной информации."
                };
                break;

            case NotFoundException notFoundException:
                code = HttpStatusCode.NotFound;
                result = new ProblemDetails
                {
                    Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title  = "Ресурс не найден.",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = notFoundException.Message
                };
                break;

            case InvalidOperationException invalidOpException:
                code = HttpStatusCode.Conflict;
                result = new ProblemDetails
                {
                    Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title  = "Операция недопустима.",
                    Status = (int)HttpStatusCode.Conflict,
                    Detail = invalidOpException.Message
                };
                break;

            case UnauthorizedAccessException unauthorizedException:
                code = HttpStatusCode.Forbidden;
                result = new ProblemDetails
                {
                    Type   = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title  = "Доступ запрещён.",
                    Status = (int)HttpStatusCode.Forbidden,
                    Detail = "У вас нет прав для выполнения этого действия."
                };
                break;

            default:
                code = HttpStatusCode.InternalServerError;

                var detail = _env.IsDevelopment()
                    ? $"{exception.Message}\n\nInner: {exception.InnerException?.Message}\n\n{exception.StackTrace}"
                    : "Произошла непредвиденная ошибка. Попробуйте позже или обратитесь к администратору.";
                result = new ProblemDetails
                {
                    Type   = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title  = "Внутренняя ошибка сервера.",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Detail = detail
                };
                break;
        }

        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.StatusCode  = (int)code;

        return context.Response.WriteAsJsonAsync(result, _jsonOptions);
    }
}
