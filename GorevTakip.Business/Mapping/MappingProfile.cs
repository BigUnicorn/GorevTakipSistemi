using AutoMapper;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using System;

namespace GorevTakip.Business.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // TaskItem -> TaskResponseDto Çevirisi
            CreateMap<TaskItem, TaskResponseDto>()
                // Özel alanlar (AssignedUserName ve IsOverdue) için özel atama kuralları yazıyoruz
                .ForMember(dest => dest.AssignedUserName, opt => 
                    opt.MapFrom(src => src.AssignedUser != null ? $"{src.AssignedUser.FirstName} {src.AssignedUser.LastName}" : "Bilinmiyor"))
                .ForMember(dest => dest.IsOverdue, opt => 
                    opt.MapFrom(src => src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow && src.Status != WorkStatus.Done));

            // TaskCreateDto -> TaskItem Çevirisi
            CreateMap<TaskCreateDto, TaskItem>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => WorkStatus.Todo)) // Varsayılan durum ataması
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow)); // Varsayılan tarih ataması
        }
    }
}