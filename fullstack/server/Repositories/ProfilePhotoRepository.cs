using fullstackapplication.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Repositories;

public sealed record ProfilePhotoData(byte[] Content, string ContentType);

public sealed class ProfilePhotoRepository(ApplicationDbContext dbContext)
{
    public Task<ProfilePhotoData?> GetAsync(int employeeAccountId, CancellationToken cancellationToken) =>
        dbContext.EmployeeAccounts.AsNoTracking()
            .Where(account => account.Id == employeeAccountId && account.ProfilePhotoContent != null)
            .Select(account => new ProfilePhotoData(
                account.ProfilePhotoContent!,
                account.ProfilePhotoContentType!))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SaveAsync(
        int employeeAccountId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.EmployeeAccounts
            .SingleOrDefaultAsync(item => item.Id == employeeAccountId, cancellationToken);
        if (account is null)
        {
            return false;
        }

        account.ProfilePhotoContent = content;
        account.ProfilePhotoContentType = contentType;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
