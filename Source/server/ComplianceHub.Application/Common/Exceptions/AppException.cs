namespace ComplianceHub.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity '{name}' ({key}) was not found.") { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized access.") : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access forbidden.") : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class RecordInactiveException : Exception
{
    public Guid ExistingId { get; }
    public RecordInactiveException(Guid existingId, string message) : base(message)
    {
        ExistingId = existingId;
    }
}
