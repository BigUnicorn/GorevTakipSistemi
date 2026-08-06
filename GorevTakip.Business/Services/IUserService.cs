using System.Threading.Tasks;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task UpdateUserRoleAsync(UserRoleUpdateDto updateDto);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(User user);
    }
}