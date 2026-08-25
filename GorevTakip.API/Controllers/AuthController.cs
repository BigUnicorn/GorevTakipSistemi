using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;
using GorevTakip.Business.Services;
using GorevTakip.Entities.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GorevTakip.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        private void SetTokenCookies(string accessToken, string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // HTTPS / Proxy Arkası
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            
            Response.Cookies.Append("accessToken", accessToken, cookieOptions);
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthLimit")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            await _authService.RegisterAsync(registerDto);
            return Ok("Kayıt başarılı.");
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthLimit")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var tokenDto = await _authService.LoginAsync(loginDto);
            SetTokenCookies(tokenDto.AccessToken, tokenDto.RefreshToken);
            return Ok(new { tokenDto.UserId, tokenDto.FirstName, tokenDto.LastName, tokenDto.Email, tokenDto.Role });
        }

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh()
        {
            var accessToken = Request.Cookies["accessToken"];
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest("Geçersiz yenileme isteği.");
            }

            var tokenDto = await _authService.RefreshTokenAsync(accessToken, refreshToken);
            SetTokenCookies(tokenDto.AccessToken, tokenDto.RefreshToken);
            return Ok(new { tokenDto.UserId, tokenDto.FirstName, tokenDto.LastName, tokenDto.Email, tokenDto.Role });
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Çıkış başarılı." });
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new 
            { 
                UserId = user.Id, 
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email, 
                Role = (int)user.Role
            });
        }
    }
}