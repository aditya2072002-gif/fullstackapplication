namespace fullstackapplication.Server.Models;

public sealed class UserNotification
{
    public int Id { get; set; }

    public int RecipientEmployeeAccountId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public int? RelatedWorkEntryId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsRead { get; set; }

    public DateTime? DismissedAtUtc { get; set; }
}
