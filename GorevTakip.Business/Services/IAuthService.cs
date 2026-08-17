using System.Threading.Tasks;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(UserRegisterDto registerDto);
        Task<TokenDto> LoginAsync(UserLoginDto loginDto);
        Task<TokenDto> RefreshTokenAsync(string token, string refreshToken);
    }
}