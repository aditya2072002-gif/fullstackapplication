namespace fullstackapplication.Server.Dtos;

public sealed record LoginResponseDto(string AccessToken, string TokenType, DateTime ExpiresAtUtc, AccountSummaryResponseDto User);
