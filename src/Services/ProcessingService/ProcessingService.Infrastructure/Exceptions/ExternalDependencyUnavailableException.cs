using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.Exceptions;

public sealed class ExternalDependencyUnavailableException : AppException
{
    public ExternalDependencyUnavailableException(string dependencyName, string message, Exception? innerException = null)
        : base($"External dependency '{dependencyName}' is unavailable. {message}", innerException ?? new InvalidOperationException(message))
    {
        DependencyName = dependencyName;
    }

    public string DependencyName { get; }
    public override string Code => "external_dependency_unavailable";
}
