namespace KabloStokTakipSistemi.Middlewares;

// Kullanıcıya döneceğimiz tek alan:
public readonly record struct ErrorBody(string Code);

public sealed record AppError(string Code, int StatusCode);

public static class AppErrors
{
    public static class Common
    {
        public static readonly AppError Unexpected   = new("KSTS-0001", StatusCodes.Status500InternalServerError);
        public static readonly AppError NotFound     = new("KSTS-0404", StatusCodes.Status404NotFound);
        public static readonly AppError Unauthorized = new("KSTS-0401", StatusCodes.Status401Unauthorized);
        public static readonly AppError Forbidden    = new("KSTS-0403", StatusCodes.Status403Forbidden); // Kimlik doğrulama yapılmış olsa bile bu işlemi yapmaya izin yok.
        public static readonly AppError Conflict     = new("KSTS-0409", StatusCodes.Status409Conflict); // Çelişme hatası
    }
    public static class Validation
    {
        public static readonly AppError BadRequest = new("KSTS-0400", StatusCodes.Status400BadRequest);
    }
    public static class Database
    {
        public static readonly AppError SqlError = new("KSTS-0601", StatusCodes.Status500InternalServerError);
    }
    public static class Mail
    {
        public static readonly AppError SendFailed = new("KSTS-0701", StatusCodes.Status502BadGateway);
    }
}

public sealed class AppException : Exception
{
    public AppError Error { get; }
    public AppException(AppError error, string? message = null, Exception? inner = null)
        : base(message, inner) => Error = error;
}