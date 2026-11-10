namespace fullstackapplication.Server.Services;

public enum ServiceResultKind
{
    Success,
    NotFound,
    Conflict,
    Invalid,
    Forbidden
}

public sealed record ServiceResult<T>(ServiceResultKind Kind, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Success(T value) => new(ServiceResultKind.Success, value);

    public static ServiceResult<T> NotFound() => new(ServiceResultKind.NotFound);

    public static ServiceResult<T> Conflict(string error) => new(ServiceResultKind.Conflict, Error: error);

    public static ServiceResult<T> Invalid(string error) => new(ServiceResultKind.Invalid, Error: error);

    public static ServiceResult<T> Forbidden(string error) => new(ServiceResultKind.Forbidden, Error: error);
}
