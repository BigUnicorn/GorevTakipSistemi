// GorevTakip.API/Filters/ValidationFilter.cs
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using FluentValidation.Results;
using System.Linq;
using System.Threading.Tasks;

namespace GorevTakip.API.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Eğer FluentValidation kuralları ihlal edildiyse ModelState.IsValid 'false' olur
            if (!context.ModelState.IsValid)
            {
                // CS8602 hatalarını gidermek için x.Value != null kontrolü ekledik ve x.Value! operatörünü kullandık
                var errors = context.ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => new ValidationFailure(x.Key, e.ErrorMessage)))
                    .ToList();

                // ExceptionMiddleware'in yakalaması için hatayı fırlatıyoruz
                throw new ValidationException(errors);
            }

            // Hata yoksa işlemi Controller'a yönlendir
            await next();
        }
    }
}