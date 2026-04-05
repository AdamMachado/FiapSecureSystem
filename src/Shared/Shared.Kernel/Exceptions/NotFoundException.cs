namespace Shared.Kernel.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public string? EntityName { get; }
    public object? Key { get; }

    public override string Code => "not_found";
}