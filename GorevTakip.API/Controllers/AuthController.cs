using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;
using GorevTakip.Business.Services;
using GorevTakip.Entities.DTOs;
using System.Threading.Tasks;

namespace GorevTakip.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthLimit")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            await _authService.RegisterAsync(registerDto);
            return Ok("Kayıt başarılı.");
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthLimit")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var tokenDto = await _authService.LoginAsync(loginDto);
            return Ok(tokenDto); // Return TokenDto (AccessToken and RefreshToken)
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto tokenDto)
        {
            if (tokenDto == null || string.IsNullOrEmpty(tokenDto.AccessToken) || string.IsNullOrEmpty(tokenDto.RefreshToken))
            {
                return BadRequest("Invalid client request");
            }

            var newTokenDto = await _authService.RefreshTokenAsync(tokenDto.AccessToken, tokenDto.RefreshToken);
            return Ok(newTokenDto);
        }
    }
}