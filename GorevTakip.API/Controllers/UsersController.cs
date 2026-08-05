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

        public UsersController(IGenericRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        // YENİ EKLENEN GET METODU (Frontend'in kullanıcıları çekebilmesi için)
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(users);
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