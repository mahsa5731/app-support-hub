namespace AppSupportHub.Application.Abstractions.Results;

public sealed class ApplicationResult<T>
{
    private readonly SuccessValue? _successValue;

    internal ApplicationResult(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _successValue = new SuccessValue(value);
        IsSuccess = true;
    }

    internal ApplicationResult(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public bool IsSuccess { get; }

    public ApplicationError? Error { get; }

    public T Value
    {
        get
        {
            if (_successValue is null)
            {
                throw new InvalidOperationException("A failed result does not contain a success value.");
            }

            return _successValue.Value;
        }
    }

    private sealed record SuccessValue(T Value);
}
