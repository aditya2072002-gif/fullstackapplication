using fullstackapplication.Server.Dtos;

namespace fullstackapplication.Server.Contracts;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, AccountSummaryResponse User);
