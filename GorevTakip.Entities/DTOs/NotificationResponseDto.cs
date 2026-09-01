using System;

namespace GorevTakip.Entities.DTOs
{
    public record NotificationResponseDto(
        int Id,
        string Message,
        bool IsRead,
        DateTime CreatedAt,
        int? RelatedTaskId
    );
}
