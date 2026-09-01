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
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IGenericRepository<User> userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task UpdateUserRoleAsync(UserRoleUpdateDto updateDto)
        {
            var user = await _userRepository.GetByIdAsync(updateDto.UserId);
            if (user == null)
                throw new System.Exception("Kullanıcı bulunamadı.");

            user.Role = updateDto.NewRole;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }
    }
}