namespace fullstackapplication.Server.Dtos;

public sealed record NotificationResponseDto(
    int Id,
    string Message,
    string Details,
    int? RelatedWorkEntryId,
    DateTime CreatedAtUtc,
    bool IsRead);
