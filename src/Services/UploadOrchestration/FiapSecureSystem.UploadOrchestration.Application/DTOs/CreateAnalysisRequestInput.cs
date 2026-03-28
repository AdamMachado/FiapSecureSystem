using System;
using System.Collections.Generic;
using System.Text;

namespace FiapSecureSystem.UploadOrchestration.Application.DTOs
{
    public record CreateAnalysisRequestInput(
        string FileName,
        string ContentType,
        Stream FileStream,
        string CorrelationId);
}
