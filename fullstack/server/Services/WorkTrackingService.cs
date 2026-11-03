using fullstackapplication.Server.Dtos;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;

namespace fullstackapplication.Server.Services;

public sealed class WorkTrackingService(IWorkTrackingRepository repository) : IWorkTrackingService
{
    public async Task<IReadOnlyList<CandidateWorkEntryResponse>> GetCandidateEntriesAsync(
        int accountId, CancellationToken cancellationToken) =>
        (await repository.GetCandidateEntriesAsync(accountId, cancellationToken)).Select(ToResponse).ToArray();

    public async Task<ServiceResult<CandidateWorkEntryResponse>> CreateAsync(
        int accountId, CandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
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
        return ServiceResult<CandidateWorkEntryResponse>.Success(ToResponse(savedEntry));
    }

    public async Task<ServiceResult<CandidateWorkEntryResponse>> UpdateCandidateEntryAsync(
        int accountId, int entryId, CandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await repository.GetCandidateEntryAsync(accountId, entryId, cancellationToken);
        if (entry is null) return ServiceResult<CandidateWorkEntryResponse>.NotFound();
        ApplyCandidateUpdate(entry, request);
        entry.Grade = null;
        entry.AdminFeedback = null;
        entry.ReviewedAtUtc = null;
        entry.SubmittedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateWorkEntryResponse>.Success(ToResponse(entry));
    }

    public async Task<ServiceResult<bool>> DeleteCandidateEntryAsync(
        int accountId, int entryId, CancellationToken cancellationToken)
    {
        var entry = await repository.GetCandidateEntryAsync(accountId, entryId, cancellationToken);
        if (entry is null) return ServiceResult<bool>.NotFound();
        if (entry.Grade.HasValue)
        {
            return ServiceResult<bool>.Conflict("A reviewed work entry cannot be deleted.");
        }

        repository.Remove(entry);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<CandidateWorkEntryResponse>> SearchAsync(
        int? candidateId, string? name, string? stack, DateOnly? dateFrom, DateOnly? dateTo,
        int? grade, CancellationToken cancellationToken) =>
        (await repository.SearchEntriesAsync(candidateId, name, stack, dateFrom, dateTo, grade, cancellationToken))
        .Select(ToResponse).ToArray();

    public async Task<ServiceResult<CandidateWorkEntryResponse>> ReviewAsync(
        int entryId, ReviewCandidateWorkEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await repository.GetEntryAsync(entryId, cancellationToken);
        if (entry is null) return ServiceResult<CandidateWorkEntryResponse>.NotFound();
        entry.Grade = request.Grade;
        entry.AdminFeedback = request.AdminFeedback?.Trim();
        entry.ReviewedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CandidateWorkEntryResponse>.Success(ToResponse(entry));
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
        entry.Grade,
        entry.AdminFeedback,
        entry.SubmittedAtUtc,
        entry.ReviewedAtUtc);
}
