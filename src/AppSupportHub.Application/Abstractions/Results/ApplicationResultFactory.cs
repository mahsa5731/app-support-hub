namespace AppSupportHub.Application.Abstractions.Results;

public static class ApplicationResultFactory
{
    public static ApplicationResult<T> Success<T>(T value)
    {
        return new ApplicationResult<T>(value);
    }

    public static ApplicationResult<T> Failure<T>(ApplicationError error)
    {
        return new ApplicationResult<T>(error);
    }
}
