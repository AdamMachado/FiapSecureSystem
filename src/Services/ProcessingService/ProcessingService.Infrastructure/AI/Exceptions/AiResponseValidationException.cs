using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.AI.Exceptions;

public sealed class AiResponseValidationException : AppException
{
    public AiResponseValidationException(string message)
        : base(message)
    {
    }

    public override string Code => "ai_response_validation_error";
}
