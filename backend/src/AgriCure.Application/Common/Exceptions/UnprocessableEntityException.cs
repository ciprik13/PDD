namespace AgriCure.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request is syntactically valid but cannot be processed due to semantic / domain rules.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public sealed class UnprocessableEntityException : Exception
{
    /// <summary>Property-level errors keyed by property name (camelCase recommended).</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public UnprocessableEntityException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage],
        };
    }

    public UnprocessableEntityException(IDictionary<string, string[]> errors)
        : base("One or more domain validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
