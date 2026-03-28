using FiapSecureSystem.UploadOrchestration.Application.Abstractions;
using FiapSecureSystem.UploadOrchestration.Application.DTOs;
using FiapSecureSystem.UploadOrchestration.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FiapSecureSystem.UploadOrchestration.Api.Controllers;

[ApiController]
[Route("api/analyses")]
public class AnalysesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        IFormFile file,
        [FromServices] CreateAnalysisRequestUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Arquivo não informado.");

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return BadRequest("Tipo de arquivo não suportado.");

        var correlationId = HttpContext.TraceIdentifier;

        await using var stream = file.OpenReadStream();

        var result = await useCase.ExecuteAsync(
            new CreateAnalysisRequestInput(
                file.FileName,
                file.ContentType,
                stream,
                correlationId),
            cancellationToken);

        return Accepted(new
        {
            analysisId = result.AnalysisId,
            status = result.Status
        });

    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(
    Guid id,
    [FromServices] IAnalysisRequestRepository repository,
    CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(id, cancellationToken);

        if (request is null)
            return NotFound();

        return Ok(new
        {
            analysisId = request.Id,
            status = request.Status.ToString(),
            errorMessage = request.ErrorMessage
        });
    }
}