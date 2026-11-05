using fullstackapplication.Server.Models;

namespace fullstackapplication.Server.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<int>> GetActiveAdminIdsAsync(CancellationToken cancellationToken);

    Task<string?> GetAccountDisplayNameAsync(int accountId, CancellationToken cancellationToken);

    Task AddCandidateCreatedNotificationsAsync(
        string candidateName,
        string employeeName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserNotification>> GetActiveForRecipientAsync(
        int recipientAccountId,
        CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyCollection<UserNotification> notifications, CancellationToken cancellationToken);

    Task MarkAllAsReadAsync(int recipientAccountId, CancellationToken cancellationToken);

    Task<bool> DismissAsync(int recipientAccountId, int notificationId, CancellationToken cancellationToken);
}
