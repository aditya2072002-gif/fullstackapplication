using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using fullstackapplication.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace fullstackapplication.Server.Services;

public sealed class NotificationService(
    INotificationRepository repository,
    IHubContext<NotificationHub> hubContext) : INotificationService
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
        await PublishAsync(notifications, cancellationToken);
    }

    public async Task NotifyCandidateCreatedAsync(
        string candidateName,
        string employeeName,
        CancellationToken cancellationToken)
    {
        var notifications = await repository.AddCandidateCreatedNotificationsAsync(candidateName, employeeName, cancellationToken);
        await PublishAsync(notifications, cancellationToken);
    }

    public async Task NotifyReviewCompletedAsync(
        int candidateAccountId,
        int reviewerAccountId,
        int workEntryId,
        string task,
        string status,
        string? comment,
        CancellationToken cancellationToken)
    {
        var reviewerName = await repository.GetAccountDisplayNameAsync(reviewerAccountId, cancellationToken)
            ?? throw new InvalidOperationException("The reviewing employee could not be found.");
        var details = $"Status: {status}\nComment: {(string.IsNullOrWhiteSpace(comment) ? "No comment provided" : comment)}";
        var notifications = new[]
        {
            new UserNotification
            {
                RecipientEmployeeAccountId = candidateAccountId,
                Message = $"{reviewerName} reviewed \"{task}\"",
                Details = details,
                RelatedWorkEntryId = workEntryId,
                CreatedAtUtc = DateTime.UtcNow
            }
        };
        await repository.AddRangeAsync(notifications, cancellationToken);
        await PublishAsync(notifications, cancellationToken);
    }

    public async Task NotifyInterviewDecisionAsync(
        int candidateAccountId,
        string decision,
        string? comment,
        CancellationToken cancellationToken)
    {
        var notifications = new[]
        {
            new UserNotification
            {
                RecipientEmployeeAccountId = candidateAccountId,
                Message = "Your interview qualification was updated",
                Details = $"Decision: {decision}\nComment: {(string.IsNullOrWhiteSpace(comment) ? "No comment provided" : comment)}",
                CreatedAtUtc = DateTime.UtcNow
            }
        };
        await repository.AddRangeAsync(notifications, cancellationToken);
        await PublishAsync(notifications, cancellationToken);
    }

    private Task PublishAsync(IReadOnlyCollection<UserNotification> notifications, CancellationToken cancellationToken) =>
        Task.WhenAll(notifications.Select(notification => hubContext.Clients
            .Group(NotificationHub.GetUserGroup(notification.RecipientEmployeeAccountId))
            .SendAsync("notification", new NotificationResponseDto(
                notification.Id,
                notification.Message,
                notification.Details,
                notification.RelatedWorkEntryId,
                notification.CreatedAtUtc,
                notification.IsRead), cancellationToken)));
}
