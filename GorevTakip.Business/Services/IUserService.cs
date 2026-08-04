using System.Threading.Tasks;
using GorevTakip.Entities;

namespace GorevTakip.Business.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
    }
}