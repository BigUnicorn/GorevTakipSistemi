using FluentValidation;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.ValidationRules
{
    public class TaskCreateDtoValidator : AbstractValidator<TaskCreateDto>
    {
        public TaskCreateDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Görev başlığı boş bırakılamaz.")
                .MaximumLength(100).WithMessage("Görev başlığı en fazla 100 karakter olabilir.");

            RuleFor(x => x.AssignedUserId)
                .GreaterThan(0).WithMessage("Görev için geçerli bir kullanıcı atanmalıdır.");

            RuleFor(x => x.Category)
                .IsInEnum().WithMessage("Geçerli bir kategori seçilmelidir.");
        }
    }
}