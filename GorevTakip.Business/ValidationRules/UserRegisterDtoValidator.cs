using FluentValidation;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.ValidationRules
{
    public class UserRegisterDtoValidator : AbstractValidator<UserRegisterDto>
    {
        public UserRegisterDtoValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");
            
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");
            
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi zorunludur.")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi formatı giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre boş olamaz.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");
        }
    }
}