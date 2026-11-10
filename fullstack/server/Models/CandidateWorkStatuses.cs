namespace fullstackapplication.Server.Models;

public static class CandidateWorkStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Approved,
        Rejected
    };
}

public static class CandidateInterviewDecisions
{
    public const string PendingReview = "PendingReview";
    public const string InterviewQualified = "InterviewQualified";
    public const string NotQualified = "NotQualified";
}
