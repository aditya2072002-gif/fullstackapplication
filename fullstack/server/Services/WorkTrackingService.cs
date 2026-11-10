using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class WorkTrackingService(
    IWorkTrackingRepository repository,
    INotificationService notificationService) : IWorkTrackingService
{
    public async Task<IReadOnlyList<CandidateWorkEntryResponse>> GetCandidateEntriesAsync(
        int accountId, CancellationToken cancellationToken) =>
        (await repository.GetCandidateEntriesAsync(accountId, cancellationToken)).Select(ToResponse).ToArray();

    public async Task<ServiceResult<CandidateWorkEntryResponse>> CreateAsync(
        int accountId, CandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
        var createdAtUtc = await repository.GetCandidateCreatedAtUtcAsync(accountId, null, cancellationToken);
        if (createdAtUtc is null) return ServiceResult<CandidateWorkEntryResponse>.NotFound();
        if (DateTime.UtcNow >= createdAtUtc.Value.AddDays(30))
        {
            return ServiceResult<CandidateWorkEntryResponse>.Conflict("The 30-day work submission window has ended.");
        }

        var entry = new CandidateWorkEntry
        {
            CandidateProfileId = accountId,
            WorkDate = request.WorkDate,
            Task = request.Task.Trim(),
            HoursSpent = request.HoursSpent,
            Accomplishment = request.Accomplishment.Trim(),
            Learnings = request.Learnings.Trim(),
            BlockersAndChallenges = request.BlockersAndChallenges.Trim(),
            AiToolsUsed = request.AiToolsUsed.Trim(),
            SubmittedAtUtc = DateTime.UtcNow
        };
        await repository.AddAsync(entry, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        var savedEntry = await repository.GetCandidateEntryAsync(accountId, entry.Id, cancellationToken)
            ?? throw new InvalidOperationException("The saved candidate work entry could not be reloaded.");
        var profile = savedEntry.CandidateProfile
            ?? throw new InvalidOperationException("The saved candidate profile could not be reloaded.");
        await notificationService.NotifyTaskSubmittedAsync(
            savedEntry.Id,
            profile.CreatorEmployeeAccountId,
            $"{profile.FirstName} {profile.LastName}",
            savedEntry.Task,
            profile.Stack,
            cancellationToken);
        return ServiceResult<CandidateWorkEntryResponse>.Success(ToResponse(savedEntry));
    }

    public async Task<ServiceResult<CandidateWorkEntryResponse>> UpdateCandidateEntryAsync(
        int accountId, int entryId, CandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await repository.GetCandidateEntryAsync(accountId, entryId, cancellationToken);
        if (entry is null) return ServiceResult<CandidateWorkEntryResponse>.NotFound();
        if (entry.Status != CandidateWorkStatuses.Pending)
        {
            return ServiceResult<CandidateWorkEntryResponse>.Conflict("A reviewed work entry cannot be changed.");
        }

        var createdAtUtc = await repository.GetCandidateCreatedAtUtcAsync(accountId, null, cancellationToken);
        if (createdAtUtc is null) return ServiceResult<CandidateWorkEntryResponse>.NotFound();
        if (DateTime.UtcNow >= createdAtUtc.Value.AddDays(30))
        {
            return ServiceResult<CandidateWorkEntryResponse>.Conflict("The 30-day work submission window has ended.");
        }

        ApplyCandidateUpdate(entry, request);
        entry.SubmittedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        var profile = entry.CandidateProfile
            ?? throw new InvalidOperationException("The candidate profile could not be reloaded for notification.");
        await notificationService.NotifyTaskSubmittedAsync(
            entry.Id,
            profile.CreatorEmployeeAccountId,
            $"{profile.FirstName} {profile.LastName}",
            entry.Task,
            profile.Stack,
            cancellationToken);
        return ServiceResult<CandidateWorkEntryResponse>.Success(ToResponse(entry));
    }

    public async Task<ServiceResult<bool>> DeleteCandidateEntryAsync(
        int accountId, int entryId, CancellationToken cancellationToken)
    {
        var entry = await repository.GetCandidateEntryAsync(accountId, entryId, cancellationToken);
        if (entry is null) return ServiceResult<bool>.NotFound();
        if (entry.Status != CandidateWorkStatuses.Pending)
        {
            return ServiceResult<bool>.Conflict("A reviewed work entry cannot be deleted.");
        }

        repository.Remove(entry);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<CandidateWorkEntryResponse>> SearchAsync(
        int? candidateId, string? name, string? stack, DateOnly? dateFrom, DateOnly? dateTo,
        string? status, int reviewerAccountId, bool isAdmin, CancellationToken cancellationToken) =>
        (await repository.SearchEntriesAsync(
            candidateId, name, stack, dateFrom, dateTo, status, isAdmin ? null : reviewerAccountId, cancellationToken))
        .Select(ToResponse).ToArray();

    public async Task<ServiceResult<IReadOnlyList<CandidateWorkEntryResponse>>> ReviewAsync(
        int reviewerAccountId,
        bool isAdmin,
        ReviewCandidateWorkEntriesRequest request,
        CancellationToken cancellationToken)
    {
        int? ownerId = isAdmin ? null : reviewerAccountId;
        var createdAtUtc = await repository.GetCandidateCreatedAtUtcAsync(request.CandidateId, ownerId, cancellationToken);
        if (createdAtUtc is null) return ServiceResult<IReadOnlyList<CandidateWorkEntryResponse>>.NotFound();

        var saved = await repository.TrySaveReviewsAsync(
            request.CandidateId,
            request.EntryIds,
            reviewerAccountId,
            request.Status,
            string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            DateTime.UtcNow,
            ownerId,
            cancellationToken);
        if (!saved)
        {
            return ServiceResult<IReadOnlyList<CandidateWorkEntryResponse>>.Conflict(
                "Every selected work entry must be pending and belong to the selected candidate.");
        }

        var reviewedEntries = await repository.SearchEntriesAsync(
            request.CandidateId, null, null, null, null, null, ownerId, cancellationToken);
        var selectedEntries = reviewedEntries.Where(entry => request.EntryIds.Contains(entry.Id)).ToArray();
        foreach (var entry in selectedEntries)
        {
            await notificationService.NotifyReviewCompletedAsync(
                request.CandidateId,
                reviewerAccountId,
                entry.Id,
                entry.Task,
                entry.Status,
                entry.AdminFeedback,
                cancellationToken);
        }

        return ServiceResult<IReadOnlyList<CandidateWorkEntryResponse>>.Success(selectedEntries.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<bool>> UpdateInterviewDecisionAsync(
        int reviewerAccountId,
        bool isAdmin,
        int candidateId,
        UpdateCandidateInterviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        int? ownerId = isAdmin ? null : reviewerAccountId;
        var createdAtUtc = await repository.GetCandidateCreatedAtUtcAsync(candidateId, ownerId, cancellationToken);
        if (createdAtUtc is null) return ServiceResult<bool>.NotFound();
        if (DateTime.UtcNow < createdAtUtc.Value.AddDays(30))
        {
            return ServiceResult<bool>.Invalid("The interview decision is available after the 30-day submission window ends.");
        }

        var saved = await repository.TrySaveInterviewDecisionAsync(
            candidateId,
            request.Decision,
            string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            reviewerAccountId,
            DateTime.UtcNow,
            ownerId,
            cancellationToken);
        if (!saved) return ServiceResult<bool>.Conflict("An interview decision has already been recorded.");
        await notificationService.NotifyInterviewDecisionAsync(
            candidateId, request.Decision, request.Comment, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static void ApplyCandidateUpdate(CandidateWorkEntry entry, CandidateWorkEntryRequest request)
    {
        entry.WorkDate = request.WorkDate;
        entry.Task = request.Task.Trim();
        entry.HoursSpent = request.HoursSpent;
        entry.Accomplishment = request.Accomplishment.Trim();
        entry.Learnings = request.Learnings.Trim();
        entry.BlockersAndChallenges = request.BlockersAndChallenges.Trim();
        entry.AiToolsUsed = request.AiToolsUsed.Trim();
    }

    private static CandidateWorkEntryResponse ToResponse(CandidateWorkEntry entry) => new(
        entry.Id,
        entry.CandidateProfileId,
        entry.CandidateProfile?.FirstName ?? string.Empty,
        entry.CandidateProfile?.LastName ?? string.Empty,
        entry.CandidateProfile?.Stack ?? string.Empty,
        entry.WorkDate,
        entry.Task,
        entry.HoursSpent,
        entry.Accomplishment,
        entry.Learnings,
        entry.BlockersAndChallenges,
        entry.AiToolsUsed,
        entry.Status,
        entry.AdminFeedback,
        entry.SubmittedAtUtc,
        entry.ReviewedAtUtc,
        entry.ReviewedByEmployeeAccountId,
        entry.ReviewedByEmployeeAccount?.DisplayName,
        entry.CandidateProfile?.CreatorEmployeeAccountId,
        entry.CandidateProfile?.CreatorEmployeeAccount?.DisplayName);
}
