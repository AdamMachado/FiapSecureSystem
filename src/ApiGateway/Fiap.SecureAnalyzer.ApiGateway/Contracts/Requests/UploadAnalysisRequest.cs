using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureAnalyzer.ApiGateway.Contracts.Requests;

public sealed class UploadAnalysisRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
