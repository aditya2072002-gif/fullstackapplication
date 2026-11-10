using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationResponseDto>> GetNotificationsAsync(
        int recipientAccountId,
        CancellationToken cancellationToken);

    Task MarkAllAsReadAsync(int recipientAccountId, CancellationToken cancellationToken);

    Task<bool> DismissAsync(int recipientAccountId, int notificationId, CancellationToken cancellationToken);

    Task<string?> GetAccountDisplayNameAsync(int accountId, CancellationToken cancellationToken);

    Task NotifyTaskSubmittedAsync(
        int workEntryId,
        int? mentorAccountId,
        string candidateName,
        string task,
        string stack,
        CancellationToken cancellationToken);

    Task NotifyCandidateCreatedAsync(
        string candidateName,
        string employeeName,
        CancellationToken cancellationToken);

    Task NotifyReviewCompletedAsync(
        int candidateAccountId,
        int reviewerAccountId,
        int workEntryId,
        string task,
        string status,
        string? comment,
        CancellationToken cancellationToken);

    Task NotifyInterviewDecisionAsync(
        int candidateAccountId,
        string decision,
        string? comment,
        CancellationToken cancellationToken);
}
