using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

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
                // İstek sorunsuzsa bir sonraki adıma geçir
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Hata oluşursa logla ve frontend'e standart bir cevap dön
                _logger.LogError($"Bir hata oluştu: {ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // 500

            // Sadece servislerden (iş kurallarından) gelen özel hata mesajlarını 400 (Bad Request) olarak dönmek istersen buraya if blokları eklenebilir. 
            // Şimdilik standart bir yapı kuruyoruz.
            
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "İşlem sırasında bir hata oluştu.",
                Detailed = exception.Message // Frontend'de hatanın ne olduğunu görebilmen için eklendi.
            };

            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}