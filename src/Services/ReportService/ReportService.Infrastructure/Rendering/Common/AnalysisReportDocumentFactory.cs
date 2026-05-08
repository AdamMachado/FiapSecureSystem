using ReportService.Application.Abstractions.Rendering;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ReportService.Infrastructure.Rendering.Common;

internal static class AnalysisReportDocumentFactory
{
    public static ReportDocument Create(RenderReportRequest request)
    {
        var analysis = request.AnalysisResult;

        return new ReportDocument(
            "Relatório Técnico de Análise de Arquitetura",
            new[]
            {
                CreateRequestSection(request),
                CreateSummarySection(analysis),
                CreateComponentsSection(analysis),
                CreateRisksSection(analysis),
                CreateRecommendationsSection(analysis)
            });
    }

    private static ReportSection CreateRequestSection(RenderReportRequest request)
    {
        return new ReportSection(
            "Informações da Solicitação",
            new IReportBlock[]
            {
                new TableBlock(
                    Headers: new[] { "Campo", "Valor" },
                    Rows:
                    [
                        new[] { "Identificador da análise", request.AnalysisRequestId.ToString() },
                        new[] { "Usuário solicitante", request.RequestedByUserId.ToString() },
                        new[] { "Formato solicitado", request.Format.ToString().ToUpperInvariant() }
                    ],
                    EmptyState: "Sem informações da solicitação.")
            });
    }

    private static ReportSection CreateSummarySection(AnalysisResultDto analysis)
    {
        return new ReportSection(
            "Resumo Executivo",
            new IReportBlock[]
            {
                new ParagraphBlock(ValueOrFallback(analysis.Summary.Overview)),
                new TableBlock(
                    Headers: new[] { "Indicador", "Valor" },
                    Rows:
                    [
                        new[] { "Total de componentes", analysis.Summary.TotalComponents.ToString() },
                        new[] { "Total de riscos", analysis.Summary.TotalRisks.ToString() },
                        new[] { "Total de recomendações", analysis.Summary.TotalRecommendations.ToString() },
                        new[] { "Requer revisão manual", analysis.Summary.RequiresManualReview ? "Sim" : "Não" }
                    ],
                    EmptyState: "Sem indicadores consolidados.",
                    Title: "Indicadores"),
                new BulletListBlock(
                    Items: analysis.Summary.Warnings.ToArray(),
                    EmptyState: "Nenhum alerta identificado.",
                    Title: "Alertas")
            });
    }

    private static ReportSection CreateComponentsSection(AnalysisResultDto analysis)
    {
        var components = analysis.Components.ToArray();

        return new ReportSection(
            "Componentes Identificados",
            new IReportBlock[]
            {
                new TableBlock(
                    Headers: new[] { "Id", "Nome", "Tipo", "Descrição" },
                    Rows: components
                        .Select(component => (IReadOnlyCollection<string>)new[]
                        {
                            component.Id,
                            component.Name,
                            component.Type.ToString(),
                            ValueOrFallback(component.Description)
                        })
                        .ToArray(),
                    EmptyState: "Nenhum componente identificado.",
                    Title: "Visão geral")
            },
            components.Select(CreateComponentSection).ToArray());
    }

    private static ReportSection CreateComponentSection(IdentifiedComponentDto component)
    {
        return new ReportSection(
            component.Name,
            new IReportBlock[]
            {
                new TableBlock(
                    Headers: new[] { "Campo", "Valor" },
                    Rows:
                    [
                        new[] { "Id", component.Id },
                        new[] { "Tipo", component.Type.ToString() },
                        new[] { "Descrição", ValueOrFallback(component.Description) }
                    ],
                    EmptyState: "Sem detalhes do componente."),
                new BulletListBlock(
                    Items: component.Tags.ToArray(),
                    EmptyState: "Nenhuma tag associada.",
                    Title: "Tags"),
                new BulletListBlock(
                    Items: component.ConnectedTo.ToArray(),
                    EmptyState: "Nenhuma conexão informada.",
                    Title: "Conexões"),
                new BulletListBlock(
                    Items: component.Metadata?
                        .Select(pair => $"{pair.Key}: {pair.Value}")
                        .ToArray() ?? Array.Empty<string>(),
                    EmptyState: "Nenhum metadado informado.",
                    Title: "Metadados")
            });
    }

    private static ReportSection CreateRisksSection(AnalysisResultDto analysis)
    {
        var risks = analysis.Risks.ToArray();

        return new ReportSection(
            "Riscos Arquiteturais",
            new IReportBlock[]
            {
                new TableBlock(
                    Headers:
                    [
                        "Id",
                        "Título",
                        "Severidade",
                        "Componente afetado",
                        "Impacto",
                        "Probabilidade"
                    ],
                    Rows: risks
                        .Select(risk => (IReadOnlyCollection<string>)new[]
                        {
                            risk.Id,
                            risk.Title,
                            risk.Severity.ToString(),
                            ValueOrFallback(risk.AffectedComponentName ?? risk.AffectedComponentId),
                            ValueOrFallback(risk.Impact),
                            ValueOrFallback(risk.Likelihood)
                        })
                        .ToArray(),
                    EmptyState: "Nenhum risco arquitetural identificado.",
                    Title: "Visão geral")
            },
            risks.Select(CreateRiskSection).ToArray());
    }

    private static ReportSection CreateRiskSection(ArchitecturalRiskDto risk)
    {
        return new ReportSection(
            risk.Title,
            new IReportBlock[]
            {
                new ParagraphBlock(
                    Text: ValueOrFallback(risk.Description),
                    Title: "Descrição"),
                new TableBlock(
                    Headers: new[] { "Campo", "Valor" },
                    Rows:
                    [
                        new[] { "Id", risk.Id },
                        new[] { "Severidade", risk.Severity.ToString() },
                        new[] { "Componente afetado", ValueOrFallback(risk.AffectedComponentName) },
                        new[] { "Id do componente afetado", ValueOrFallback(risk.AffectedComponentId) },
                        new[] { "Impacto", ValueOrFallback(risk.Impact) },
                        new[] { "Probabilidade", ValueOrFallback(risk.Likelihood) }
                    ],
                    EmptyState: "Sem detalhes do risco.",
                    Title: "Detalhes do risco"),
                new BulletListBlock(
                    Items: risk.Evidence.ToArray(),
                    EmptyState: "Nenhuma evidência informada.",
                    Title: "Evidências")
            });
    }

    private static ReportSection CreateRecommendationsSection(AnalysisResultDto analysis)
    {
        var recommendations = analysis.Recommendations.ToArray();

        return new ReportSection(
            "Recomendações",
            new IReportBlock[]
            {
                new TableBlock(
                    Headers:
                    [
                        "Id",
                        "Título",
                        "Categoria",
                        "Prioridade",
                        "Risco relacionado",
                        "Componente alvo"
                    ],
                    Rows: recommendations
                        .Select(recommendation => (IReadOnlyCollection<string>)new[]
                        {
                            recommendation.Id,
                            recommendation.Title,
                            recommendation.Category.ToString(),
                            recommendation.Priority.ToString(),
                            ValueOrFallback(recommendation.RelatedRiskId),
                            ValueOrFallback(recommendation.TargetComponentId)
                        })
                        .ToArray(),
                    EmptyState: "Nenhuma recomendação identificada.",
                    Title: "Visão geral")
            },
            recommendations.Select(CreateRecommendationSection).ToArray());
    }

    private static ReportSection CreateRecommendationSection(ArchitecturalRecommendationDto recommendation)
    {
        return new ReportSection(
            recommendation.Title,
            new IReportBlock[]
            {
                new ParagraphBlock(
                    Text: ValueOrFallback(recommendation.Description),
                    Title: "Descrição"),
                new TableBlock(
                    Headers: new[] { "Campo", "Valor" },
                    Rows:
                    [
                        new[] { "Id", recommendation.Id },
                        new[] { "Categoria", recommendation.Category.ToString() },
                        new[] { "Prioridade", recommendation.Priority.ToString() },
                        new[] { "Risco relacionado", ValueOrFallback(recommendation.RelatedRiskId) },
                        new[] { "Componente alvo", ValueOrFallback(recommendation.TargetComponentId) }
                    ],
                    EmptyState: "Sem detalhes da recomendação.",
                    Title: "Detalhes da recomendação"),
                new BulletListBlock(
                    Items: recommendation.ExpectedBenefits.ToArray(),
                    EmptyState: "Nenhum benefício esperado informado.",
                    Title: "Benefícios esperados")
            });
    }

    private static string ValueOrFallback(string? value)
        => string.IsNullOrWhiteSpace(value) ? "Não informado." : value.Trim();
}
