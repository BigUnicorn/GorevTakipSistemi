using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GorevTakip.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IGenericRepository<User> userRepository, IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(UserRegisterDto registerDto)
        {
            var existingUsers = await _userRepository.GetAllAsync();
            if (existingUsers.Any(u => u.Email == registerDto.Email))
                throw new Exception("Bu email adresi zaten kullanılıyor.");

            // Şifreyi Hash'leme işlemi (Basit SHA256)
            var passwordHash = HashPassword(registerDto.Password);

            var newUser = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.Employee // Varsayılan olarak personel atıyoruz
            };

            await _userRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string> LoginAsync(UserLoginDto loginDto)
        {
            var allUsers = await _userRepository.GetAllAsync();
            var user = allUsers.FirstOrDefault(u => u.Email == loginDto.Email);

            if (user == null || user.PasswordHash != HashPassword(loginDto.Password))
                throw new Exception("Kullanıcı adı veya şifre hatalı.");

            return GenerateJwtToken(user);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private string GenerateJwtToken(User user)
        {
            // appsettings.json'dan gizli anahtarı alıyoruz
            var jwtKey = _configuration["Jwt:Key"] ?? "GorevTakipSistemi_SuperGizliAnahtar_12345!!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // DEĞİŞTİRİLEN KISIM BURASI:
            // Token içine kullanıcının kimlik bilgilerini (Id, Email, Rol) gömüyoruz
            var claims = new[]
            {
                // Controller tarafında User.FindFirst(ClaimTypes.NameIdentifier) ile okuyabilmek için:
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                
                // E-posta bilgisi:
                new Claim(ClaimTypes.Email, user.Email),
                
                // Rol bilgisi (Admin veya Employee/User):
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // Token 2 saat geçerli
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}