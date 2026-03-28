using System;
using System.Collections.Generic;
using System.Text;

namespace FiapSecureSystem.UploadOrchestration.Application.DTOs
{
    public record CreateAnalysisRequestOutput(
        Guid AnalysisId,
        string Status);
}
