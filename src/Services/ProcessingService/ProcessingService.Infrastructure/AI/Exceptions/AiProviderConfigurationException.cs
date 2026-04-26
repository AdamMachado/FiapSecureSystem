using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.AI.Exceptions;

public sealed class AiProviderConfigurationException : AppException
{
    public AiProviderConfigurationException(string message)
        : base(message)
    {
    }

    public override string Code => "ai_provider_configuration_error";
}
