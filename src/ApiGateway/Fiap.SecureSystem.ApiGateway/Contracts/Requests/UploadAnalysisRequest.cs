using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureSystem.ApiGateway.Contracts.Requests;

public sealed class UploadAnalysisRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
