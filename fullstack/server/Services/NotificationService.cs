using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class NotificationService(INotificationRepository repository) : INotificationService
{
    public async Task<IReadOnlyList<NotificationResponseDto>> GetNotificationsAsync(
        int recipientAccountId,
        CancellationToken cancellationToken)
    {
        var notifications = await repository.GetActiveForRecipientAsync(recipientAccountId, cancellationToken);
        return notifications.Select(notification => new NotificationResponseDto(
            notification.Id,
            notification.Message,
            notification.Details,
            notification.RelatedWorkEntryId,
            notification.CreatedAtUtc,
            notification.IsRead)).ToArray();
    }

    public Task MarkAllAsReadAsync(int recipientAccountId, CancellationToken cancellationToken) =>
        repository.MarkAllAsReadAsync(recipientAccountId, cancellationToken);

    public Task<bool> DismissAsync(int recipientAccountId, int notificationId, CancellationToken cancellationToken) =>
        repository.DismissAsync(recipientAccountId, notificationId, cancellationToken);

    public Task<string?> GetAccountDisplayNameAsync(int accountId, CancellationToken cancellationToken) =>
        repository.GetAccountDisplayNameAsync(accountId, cancellationToken);

    public async Task NotifyTaskSubmittedAsync(
        int workEntryId,
        int? mentorAccountId,
        string candidateName,
        string task,
        string stack,
        CancellationToken cancellationToken)
    {
        var recipientIds = await repository.GetActiveAdminIdsAsync(cancellationToken);
        var recipients = mentorAccountId.HasValue
            ? recipientIds.Append(mentorAccountId.Value).Distinct().ToArray()
            : recipientIds.ToArray();
        var details = $"Stack: {(string.IsNullOrWhiteSpace(stack) ? "Not provided" : stack)}";
        var createdAtUtc = DateTime.UtcNow;
        var notifications = recipients.Select(accountId => new UserNotification
        {
            RecipientEmployeeAccountId = accountId,
            Message = $"{candidateName} submitted \"{task}\"",
            Details = details,
            RelatedWorkEntryId = workEntryId,
            CreatedAtUtc = createdAtUtc
        }).ToArray();

        await repository.AddRangeAsync(notifications, cancellationToken);
    }

    public Task NotifyCandidateCreatedAsync(
        string candidateName,
        string employeeName,
        CancellationToken cancellationToken) =>
        repository.AddCandidateCreatedNotificationsAsync(candidateName, employeeName, cancellationToken);

    public async Task NotifyReviewCompletedAsync(
        int candidateAccountId,
        int reviewerAccountId,
        int workEntryId,
        string task,
        int grade,
        string? feedback,
        CancellationToken cancellationToken)
    {
        var reviewerName = await repository.GetAccountDisplayNameAsync(reviewerAccountId, cancellationToken)
            ?? throw new InvalidOperationException("The reviewing employee could not be found.");
        var details = $"Grade: {grade}/100\nFeedback: {(string.IsNullOrWhiteSpace(feedback) ? "No feedback provided" : feedback)}";
        await repository.AddRangeAsync(
            [new UserNotification
            {
                RecipientEmployeeAccountId = candidateAccountId,
                Message = $"{reviewerName} reviewed \"{task}\"",
                Details = details,
                RelatedWorkEntryId = workEntryId,
                CreatedAtUtc = DateTime.UtcNow
            }],
            cancellationToken);
    }
}
