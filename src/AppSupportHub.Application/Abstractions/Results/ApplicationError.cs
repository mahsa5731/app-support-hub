namespace AppSupportHub.Application.Abstractions.Results;

public sealed record ApplicationError
{
    public ApplicationError(string code, string description, ApplicationErrorType type)
    {
        Code = NormalizeRequired(code, nameof(code));
        Description = NormalizeRequired(description, nameof(description));

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Error type is not defined.");
        }

        Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ApplicationErrorType Type { get; }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
