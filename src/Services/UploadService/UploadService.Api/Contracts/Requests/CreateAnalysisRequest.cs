using Microsoft.AspNetCore.Http;

namespace UploadService.Api.Contracts.Requests;

public sealed class CreateAnalysisRequest
{
    public IFormFile File { get; init; } = default!;
}
