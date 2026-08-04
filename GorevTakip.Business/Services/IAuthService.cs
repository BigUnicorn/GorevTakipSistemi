using System.Threading.Tasks;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(UserRegisterDto registerDto);
        Task<string> LoginAsync(UserLoginDto loginDto);
    }
}