using System.Linq;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IGenericRepository<User> _userRepository;

        public UserService(IGenericRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var allUsers = await _userRepository.GetAllAsync();
            return allUsers.FirstOrDefault(u => u.Email == email);
        }

        public async Task UpdateUserRoleAsync(UserRoleUpdateDto updateDto)
        {
            var user = await _userRepository.GetByIdAsync(updateDto.UserId);
            if (user == null)
                throw new System.Exception("Kullanıcı bulunamadı.");

            user.Role = updateDto.NewRole;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return user;
        }
    }
}