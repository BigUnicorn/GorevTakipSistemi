using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation; // YENİ EKLENDİ

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
                // İstek sorunsuzsa bir sonraki adıma geç
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Hata oluşursa logla ve yakala
                _logger.LogError($"Bir hata oluştu: {ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            object response;

            // YENİ EKLENEN KISIM: FluentValidation Hatası Yakalama
            if (exception is ValidationException validationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest; // 400
                
                // Hataları gruplayıp frontend'in kolayca okuyabileceği bir listeye çeviriyoruz
                var errors = validationException.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage })
                    .ToList();

                response = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "Doğrulama hataları oluştu.",
                    Errors = errors
                };
            }
            else
            {
                // Diğer tüm sistem hataları (500)
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "İşlem sırasında sunucuda bir hata oluştu.",
                    Detailed = exception.Message // Sadece geliştirme aşamasında tutulmalı
                };
            }

            // JSON formatını camelCase (küçük harfle başlama) standardına uygun hale getiriyoruz
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);
            
            return context.Response.WriteAsync(json);
        }
    }
}