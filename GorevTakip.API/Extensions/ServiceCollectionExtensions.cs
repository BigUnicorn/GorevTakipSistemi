using System.Text;
using GorevTakip.Business.Services;
using GorevTakip.DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using GorevTakip.Business.ValidationRules;

namespace GorevTakip.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 1. Genel Repository (Generic) ve UnitOfWork Kayıtları (Mevcut kodunuz)
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddFluentValidationAutoValidation();

            services.AddValidatorsFromAssemblyContaining<TaskCreateDtoValidator>();
            
            // 2. YENİ EKLENEN SPESİFİK REPOSITORY KAYITLARI (İşte burası!)
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITaskHistoryRepository, TaskHistoryRepository>();
            services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
            services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();

            // 3. Business (Servis) Katmanı Kayıtları (Mevcut kodunuz)
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAttachmentService, AttachmentService>();

            // MediatR Kaydı
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GorevTakip.Business.Mapping.MappingProfile).Assembly));

            // AutoMapper ve Validation kayıtlarınız aşağıda aynen kalacak...
            services.AddAutoMapper(cfg => {
                cfg.AddProfile<GorevTakip.Business.Mapping.MappingProfile>();
            });

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            
            // Eğer JWT key bulunamazsa, sistem varsayılan bir şifre atamak yerine direkt çöksün.
            var jwtKey = configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "Kritik Hata: JWT Key konfigürasyon dosyasında veya .env içinde bulunamadı!");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.ContainsKey("accessToken"))
                            {
                                context.Token = context.Request.Cookies["accessToken"];
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            // Senin mevcut Swagger ayarların
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Görev Takip API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Token giriniz..."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}