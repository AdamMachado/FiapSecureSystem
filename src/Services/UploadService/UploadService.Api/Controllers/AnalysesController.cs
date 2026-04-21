using UploadService.Api.Configuration;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using UploadService.Api.Contracts.Requests;
using UploadService.Api.Contracts.Responses;
using UploadService.Application.UseCases.CreateAnalysis;
using UploadService.Application.UseCases.GetAnalysisStatus;

namespace UploadService.Api.Controllers;

[ApiController]
[Route("api/analyses")]
[Produces("application/json")]
public sealed class AnalysesController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateAnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> CreateAsync(
        [FromForm] CreateAnalysisRequest request,
        [FromServices] CreateAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.File)] = ["A file is required."]
            });
        }

        await using var fileStream = request.File.OpenReadStream();
        var fileHash = await ComputeSha256Async(fileStream, cancellationToken);
        fileStream.Position = 0;

        var command = new CreateAnalysisCommand(
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            fileStream,
            fileHash);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = new CreateAnalysisResponse(
            result.Value.AnalysisRequestId,
            result.Value.Status.ToString(),
            result.Value.CreatedAtUtc,
            Url.ActionLink(
                action: nameof(GetStatusAsync),
                controller: "Analyses",
                values: new { analysisRequestId = result.Value.AnalysisRequestId }) ??
            $"/api/analyses/{result.Value.AnalysisRequestId}");

        return Results.Created($"/api/analyses/{result.Value.AnalysisRequestId}", response);
    }

    [HttpGet("{analysisRequestId:guid}")]
    [ProducesResponseType(typeof(GetAnalysisStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> GetStatusAsync(
        Guid analysisRequestId,
        [FromServices] GetAnalysisStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAnalysisStatusQuery(analysisRequestId), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = new GetAnalysisStatusResponse(
            result.Value.AnalysisRequestId,
            result.Value.Status.ToString(),
            result.Value.FileName,
            result.Value.ContentType,
            result.Value.SizeInBytes,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc,
            result.Value.StartedAtUtc,
            result.Value.CompletedAtUtc,
            result.Value.FailedAtUtc,
            result.Value.FailureReason);

        return Results.Ok(response);
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
