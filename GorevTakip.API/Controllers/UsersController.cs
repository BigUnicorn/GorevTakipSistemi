using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GorevTakip.Business.Services;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using System.Threading.Tasks;

namespace GorevTakip.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // Artık Repository yok, sadece Service var!
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            // Controller artık sadece Service'den veri istiyor
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            var createdUser = await _userService.CreateUserAsync(user);
            return Ok(createdUser);
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRoleUpdateDto updateDto)
        {
            if (id != updateDto.UserId)
                return BadRequest("URL içindeki ID ile gönderilen ID uyuşmuyor.");

            // try-catch bloğu kaldırıldı, hata yönetimi Middleware'e devredildi.
            await _userService.UpdateUserRoleAsync(updateDto);
            return Ok("Kullanıcı rolü başarıyla güncellendi.");
        }
    }
}