using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Exceptions;
using Shared.Contracts.IntegrationEvents.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Infrastructure.AI.StubAnalysis;

public sealed class StubArchitectureAnalyzer : IArchitectureAnalyzer
{
    private readonly ILogger<StubArchitectureAnalyzer> _logger;

    public StubArchitectureAnalyzer(ILogger<StubArchitectureAnalyzer> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(DiagramType diagramType)
    {
        return diagramType is DiagramType.Pdf or DiagramType.Image;
    }

    public Task<ArchitectureAnalysisResult> AnalyzeAsync(
        ArchitectureAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanHandle(request.DiagramType))
            throw new UnsupportedDiagramFormatException(request.ContentType);

        _logger.LogWarning(
            "Using stub architecture analyzer. No external AI call will be made. AnalysisRequestId={AnalysisRequestId}, DiagramType={DiagramType}, ContentType={ContentType}",
            request.AnalysisRequestId,
            request.DiagramType,
            request.ContentType);

        var result = CreateFakeResult(request);

        Thread.Sleep(15000);

        return Task.FromResult(result);
    }

    private static ArchitectureAnalysisResult CreateFakeResult(ArchitectureAnalysisRequest request)
    {
        var components = new List<IdentifiedComponentDto>
        {
            new(
                Id: "cmp-client-web",
                Name: "Web Frontend",
                Type: ComponentType.Frontend,
                Description: "Aplicação web simulada responsável por iniciar o fluxo de upload e acompanhar o status da análise.",
                Tags: new[] { "stub", "frontend", "webapp" },
                ConnectedTo: new[] { "cmp-api-gateway" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["confidence"] = "fake-high",
                    ["analysisRequestId"] = request.AnalysisRequestId.ToString()
                }),

            new(
                Id: "cmp-api-gateway",
                Name: "API Gateway",
                Type: ComponentType.ApiGateway,
                Description: "Camada simulada de entrada para rotear chamadas para os microsserviços internos.",
                Tags: new[] { "stub", "gateway", "rest" },
                ConnectedTo: new[] { "cmp-upload-service", "cmp-processing-service", "cmp-report-service" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["protocol"] = "https"
                }),

            new(
                Id: "cmp-upload-service",
                Name: "Upload Service",
                Type: ComponentType.Backend,
                Description: "Serviço simulado responsável por receber arquivos e publicar eventos de análise solicitada.",
                Tags: new[] { "stub", "microservice", "upload" },
                ConnectedTo: new[] { "cmp-object-storage", "cmp-message-broker" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["database"] = "postgres-schema-upload"
                }),

            new(
                Id: "cmp-processing-service",
                Name: "Processing Service",
                Type: ComponentType.Backend,
                Description: "Serviço simulado responsável por processar o diagrama e emitir o resultado da análise.",
                Tags: new[] { "stub", "microservice", "ai-analysis" },
                ConnectedTo: new[] { "cmp-object-storage", "cmp-message-broker", "cmp-openai" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["aiProvider"] = "fake-openai"
                }),

            new(
                Id: "cmp-report-service",
                Name: "Report Service",
                Type: ComponentType.Backend,
                Description: "Serviço simulado responsável por consumir o resultado da análise e gerar relatório técnico.",
                Tags: new[] { "stub", "microservice", "reporting" },
                ConnectedTo: new[] { "cmp-message-broker", "cmp-object-storage" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["format"] = "json-markdown-pdf"
                }),

            new(
                Id: "cmp-message-broker",
                Name: "RabbitMQ",
                Type: ComponentType.Queue,
                Description: "Broker simulado usado para comunicação assíncrona entre os serviços.",
                Tags: new[] { "stub", "messaging", "rabbitmq" },
                ConnectedTo: new[] { "cmp-upload-service", "cmp-processing-service", "cmp-report-service" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["exchange.analysis"] = "analysis.exchange",
                    ["exchange.report"] = "report.exchange"
                }),

            new(
                Id: "cmp-object-storage",
                Name: "Object Storage",
                Type: ComponentType.Storage,
                Description: "Armazenamento simulado para arquivos enviados e relatórios gerados.",
                Tags: new[] { "stub", "minio", "storage" },
                ConnectedTo: new[] { "cmp-upload-service", "cmp-processing-service", "cmp-report-service" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["bucket"] = "analysis-uploads"
                }),

            new(
                Id: "cmp-postgres",
                Name: "PostgreSQL",
                Type: ComponentType.Database,
                Description: "Banco relacional simulado com schemas separados por serviço.",
                Tags: new[] { "stub", "database", "postgresql" },
                ConnectedTo: new[] { "cmp-upload-service", "cmp-processing-service", "cmp-report-service" },
                Metadata: new Dictionary<string, string>
                {
                    ["source"] = "stub",
                    ["isolation"] = "schema-per-service"
                })
        };

        var risks = new List<ArchitecturalRiskDto>
        {
            new(
                Id: "risk-auth-boundary",
                Title: "Autenticação e autorização não evidenciadas no diagrama",
                Description: "O diagrama simulado não deixa explícito onde autenticação, autorização e propagação de identidade são aplicadas.",
                Severity: RiskSeverityLevel.High,
                AffectedComponentId: "cmp-api-gateway",
                AffectedComponentName: "API Gateway",
                Impact: "Chamadas internas ou externas podem ocorrer sem validação consistente de identidade e permissões.",
                Likelihood: "Média em MVPs sem camada de segurança claramente modelada.",
                Evidence: new[]
                {
                    "Não há componente explícito de identity provider.",
                    "Não há indicação de validação de token no gateway.",
                    "Não há política documentada de autorização por serviço."
                }),

            new(
                Id: "risk-message-duplication",
                Title: "Possível duplicação ou reprocessamento de mensagens",
                Description: "O fluxo assíncrono depende de consumo idempotente para evitar efeitos colaterais em retries ou redelivery.",
                Severity: RiskSeverityLevel.Medium,
                AffectedComponentId: "cmp-message-broker",
                AffectedComponentName: "RabbitMQ",
                Impact: "Eventos como análise concluída ou relatório gerado podem ser processados mais de uma vez.",
                Likelihood: "Média quando não há deduplicação por EventId ou AnalysisRequestId.",
                Evidence: new[]
                {
                    "Uso de mensageria assíncrona.",
                    "Retries e DLQ fazem parte do desenho esperado.",
                    "Consumidores precisam ser idempotentes."
                }),

            new(
                Id: "risk-ai-output-validation",
                Title: "Saída de IA exige validação estrutural",
                Description: "Resultados de IA devem ser validados antes de persistência e publicação de eventos.",
                Severity: RiskSeverityLevel.High,
                AffectedComponentId: "cmp-processing-service",
                AffectedComponentName: "Processing Service",
                Impact: "Uma resposta malformada pode quebrar desserialização, persistência ou geração de relatório.",
                Likelihood: "Alta quando o provedor externo retorna JSON fora do schema esperado.",
                Evidence: new[]
                {
                    "Fluxo depende de DTO estruturado.",
                    "Resultado é publicado para outros serviços.",
                    "Relatório é gerado a partir da análise."
                })
        };

        var recommendations = new List<ArchitecturalRecommendationDto>
        {
            new(
                Id: "rec-auth-boundary",
                Title: "Explicitar boundary de segurança no gateway",
                Description: "Adicionar validação de token, propagação de correlation id e regras mínimas de autorização no API Gateway.",
                Category: RecommendationCategory.Security,
                Priority: RiskSeverityLevel.High,
                RelatedRiskId: "risk-auth-boundary",
                TargetComponentId: "cmp-api-gateway",
                ExpectedBenefits: new[]
                {
                    "Redução de chamadas não autorizadas.",
                    "Melhor rastreabilidade de usuário.",
                    "Separação clara entre tráfego externo e interno."
                }),

            new(
                Id: "rec-idempotency",
                Title: "Adicionar idempotência nos consumidores de eventos",
                Description: "Persistir EventId ou chave composta por AnalysisRequestId e tipo de evento para evitar reprocessamento indevido.",
                Category: RecommendationCategory.Reliability,
                Priority: RiskSeverityLevel.Medium,
                RelatedRiskId: "risk-message-duplication",
                TargetComponentId: "cmp-message-broker",
                ExpectedBenefits: new[]
                {
                    "Maior tolerância a retries.",
                    "Menor risco de duplicação de relatórios.",
                    "Comportamento previsível em redelivery."
                }),

            new(
                Id: "rec-ai-contract-validation",
                Title: "Validar contrato de saída antes de publicar AnalysisCompleted",
                Description: "Garantir que componentes, riscos, recomendações e resumo estejam consistentes antes de emitir o evento de conclusão.",
                Category: RecommendationCategory.Maintainability,
                Priority: RiskSeverityLevel.High,
                RelatedRiskId: "risk-ai-output-validation",
                TargetComponentId: "cmp-processing-service",
                ExpectedBenefits: new[]
                {
                    "Menos falhas de desserialização entre serviços.",
                    "Melhor isolamento de falhas externas.",
                    "Relatórios gerados com dados mínimos confiáveis."
                }),

            new(
                Id: "rec-observability",
                Title: "Manter traces e logs correlacionados no fluxo inteiro",
                Description: "Propagar CorrelationId e CausationId em HTTP, eventos e logs estruturados.",
                Category: RecommendationCategory.Observability,
                Priority: RiskSeverityLevel.Medium,
                RelatedRiskId: null,
                TargetComponentId: "cmp-processing-service",
                ExpectedBenefits: new[]
                {
                    "Diagnóstico mais simples de falhas.",
                    "Rastreamento ponta a ponta.",
                    "Melhor suporte à demonstração do hackathon."
                })
        };

        var summary = new AnalysisSummaryDto(
            Overview:
                "Resultado sintético gerado por StubArchitectureAnalyzer. Os componentes, riscos e recomendações são falsos, mas estruturalmente válidos para testar o fluxo completo sem chamadas à OpenAI.",
            TotalComponents: components.Count,
            TotalRisks: risks.Count,
            TotalRecommendations: recommendations.Count,
            RequiresManualReview: true,
            Warnings: new[]
            {
                "Resultado gerado por stub/mock.",
                "Não representa análise real do diagrama enviado.",
                "Usar apenas para testes de integração, mensageria, persistência e geração de relatório."
            });

        var analysisResult = new AnalysisResultDto(
            Components: components,
            Risks: risks,
            Recommendations: recommendations,
            Summary: summary);

        return new ArchitectureAnalysisResult(
            Components: components,
            Risks: risks,
            Recommendations: recommendations,
            ExtractedText: "Texto extraído de stub/mock para testes.",
            Overview: summary.Overview,
            RequiresManualReview: summary.RequiresManualReview,
            Warnings: summary.Warnings);
    }
}
