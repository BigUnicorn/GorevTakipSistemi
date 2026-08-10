using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation; 
using Microsoft.AspNetCore.Mvc; // ProblemDetails sınıfları için gerekli

namespace GorevTakip.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bir hata oluştu: {ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // İçerik tipini RFC 7807 standardına (Problem Details) göre ayarlıyoruz
            context.Response.ContentType = "application/problem+json";

            ProblemDetails problemDetails;

            // 1. FluentValidation Hatalarını Yakalama ve Formatlama
            if (exception is ValidationException validationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // FluentValidation hatalarını ProblemDetails'ın beklediği Dictionary yapısına çeviriyoruz
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                problemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Doğrulama Hatası",
                    Detail = "İstekte bir veya daha fazla doğrulama kuralı ihlal edildi.",
                    Instance = context.Request.Path
                };
            }
            // 2. Beklenmeyen Genel Sistem Hataları (500)
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Sunucu Hatası",
                    Detail = "İşlem sırasında sunucuda beklenmeyen bir hata oluştu.",
                    Instance = context.Request.Path
                };

                // Geliştirme ortamında detayı okuyabilmek için Extensions içine ekliyoruz
                problemDetails.Extensions["detailedMessage"] = exception.Message;
            }

            // JSON formatını dönüştürüp frontend'e gönderiyoruz
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(problemDetails, options);

            return context.Response.WriteAsync(json);
        }
    }
}