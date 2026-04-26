using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Infrastructure.AI.Inspection;
using ProcessingService.Infrastructure.AI.Options;

namespace ProcessingService.Infrastructure.AI.OpenAI;

public sealed class ArchitectureAnalysisPromptBuilder
{
    private readonly ArchitectureAnalysisOptions _options;

    public ArchitectureAnalysisPromptBuilder(IOptions<ArchitectureAnalysisOptions> options)
    {
        _options = options.Value;
    }

    public string BuildDeveloperInstructions()
    {
        // Keep this prefix stable and long enough to benefit from prompt caching.
        return $$"""
        Você é um arquiteto de software sênior especializado em análise de diagramas de arquitetura.
        Objetivo: analisar imagens ou PDFs de diagramas de arquitetura e retornar uma análise técnica estruturada.

        Regras obrigatórias:
        - Analise somente evidências visíveis ou texto extraível no arquivo.
        - Não invente serviços, bancos, filas, protocolos ou riscos que não tenham evidência razoável.
        - Quando algo estiver ambíguo, use warnings e requiresManualReview=true.
        - Identifique componentes arquiteturais ricos: atores, clientes, frontends, backends, gateways, bancos, filas, caches, storage, serviços externos, rede, segurança, observabilidade e infraestrutura.
        - Identifique conexões entre componentes quando estiverem explícitas ou fortemente indicadas.
        - Classifique riscos por severidade: Low, Medium, High, Critical.
        - Cada risco deve ter evidência objetiva baseada no diagrama ou texto.
        - Recomendações devem ser práticas, acionáveis e relacionadas aos riscos ou melhorias arquiteturais.
        - Não retorne Markdown, comentários ou texto fora do JSON estruturado.
        - Use IDs estáveis e curtos: cmp-001, risk-001, rec-001.
        - Responda em {{_options.Language}}.
        - Limites: até {{_options.MaxComponents}} componentes, {{_options.MaxRisks}} riscos, {{_options.MaxRecommendations}} recomendações e {{_options.MaxWarnings}} warnings.
        """;
    }

    public string BuildUserInstructions(ArchitectureAnalysisRequest request, AnalysisFileInspectionResult inspection)
    {
        var inspectionText = inspection.ExtractedTextPreview is null
            ? "Nenhum texto pré-extraído disponível."
            : $"Texto pré-extraído localmente do arquivo, use apenas como pista auxiliar e valide visualmente quando possível:\n{inspection.ExtractedTextPreview}";

        var warnings = inspection.Warnings.Count == 0
            ? "Nenhum warning técnico prévio."
            : string.Join("\n", inspection.Warnings.Select(static warning => $"- {warning}"));

        return $$"""
        Analise o arquivo de arquitetura enviado nesta mensagem.
        Retorne o JSON estruturado conforme schema. Seja compacto, mas mantenha evidências suficientes para auditoria.

        Warnings técnicos prévios:
        {{warnings}}

        {{inspectionText}}
        """;
    }

    /*
     // Removido do prompt por baixa relevância para a análise.
        Metadados do processamento:
        - AnalysisRequestId: {{request.AnalysisRequestId}}
        - RequestedByUserId: {{request.RequestedByUserId}}
        - SourceFileName: {{request.SourceFileName}}
        - ContentType: {{request.ContentType}}
        - DiagramType: {{request.DiagramType}}
        - SizeInBytes: {{inspection.SizeInBytes}}
        - Width: {{inspection.Width?.ToString() ?? "n/a"}}
        - Height: {{inspection.Height?.ToString() ?? "n/a"}}
        - PageCount: {{inspection.PageCount?.ToString() ?? "n/a"}}
    */
}
