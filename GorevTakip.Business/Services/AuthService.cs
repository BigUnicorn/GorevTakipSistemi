using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
// BCrypt kütüphanesini kullanacağımız için ekstra bir using eklememize gerek yok, doğrudan çağırabiliriz.

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
            if (await _userRepository.AnyAsync(u => u.Email == registerDto.Email))
                throw new Exception("Bu email adresi zaten kullanılıyor.");

            // YENİ: BCrypt ile şifreleme işlemi
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

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
            var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // YENİ: BCrypt.Verify ile düz metin şifreyi, veritabanındaki hash ile karşılaştırıyoruz
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new Exception("Kullanıcı adı veya şifre hatalı.");

            return GenerateJwtToken(user);
        }

        // ESKİ HASH METODUNU BURADAN SİLDİK

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "Kritik Hata: JWT Key konfigürasyon dosyasında veya .env içinde bulunamadı!");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), 
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}