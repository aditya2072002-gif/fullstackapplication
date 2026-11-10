using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<int>> GetActiveAdminIdsAsync(CancellationToken cancellationToken) =>
        await dbContext.EmployeeAccounts
            .AsNoTracking()
            .Where(account => account.Role == EmployeeRoles.Admin && account.IsActive)
            .Select(account => account.Id)
            .ToListAsync(cancellationToken);

    public Task<string?> GetAccountDisplayNameAsync(int accountId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts
            .AsNoTracking()
            .Where(account => account.Id == accountId && account.IsActive)
            .Select(account => account.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<UserNotification>> AddCandidateCreatedNotificationsAsync(
        string candidateName,
        string employeeName,
        CancellationToken cancellationToken)
    {
        var adminIds = await GetActiveAdminIdsAsync(cancellationToken);
        var createdAtUtc = DateTime.UtcNow;
        var notifications = adminIds.Select(adminId => new UserNotification
        {
            RecipientEmployeeAccountId = adminId,
            Message = $"{candidateName} was created by {employeeName}",
            Details = "A new candidate is ready for work tracking.",
            CreatedAtUtc = createdAtUtc
        }).ToArray();
        await AddRangeAsync(notifications, cancellationToken);
        return notifications;
    }

    public async Task<IReadOnlyList<UserNotification>> GetActiveForRecipientAsync(
        int recipientAccountId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<UserNotification>()
            .AsNoTracking()
            .Where(notification => notification.RecipientEmployeeAccountId == recipientAccountId
                && notification.DismissedAtUtc == null)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        await dbContext.Set<UserNotification>().AddRangeAsync(notifications, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(int recipientAccountId, CancellationToken cancellationToken)
    {
        var unreadNotifications = await dbContext.Set<UserNotification>()
            .Where(notification => notification.RecipientEmployeeAccountId == recipientAccountId
                && notification.DismissedAtUtc == null
                && !notification.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        if (unreadNotifications.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> DismissAsync(
        int recipientAccountId,
        int notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.Set<UserNotification>().SingleOrDefaultAsync(
            item => item.Id == notificationId
                && item.RecipientEmployeeAccountId == recipientAccountId
                && item.DismissedAtUtc == null,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.DismissedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
