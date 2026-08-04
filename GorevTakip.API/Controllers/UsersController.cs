using Microsoft.AspNetCore.Mvc;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using System.Threading.Tasks;

namespace GorevTakip.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IGenericRepository<User> _userRepository;

        // Geçici olarak direkt Repository'i alıyoruz ki hızlıca kullanıcı ekleyebilelim.
        public UsersController(IGenericRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return Ok(user);
        }
    }
}