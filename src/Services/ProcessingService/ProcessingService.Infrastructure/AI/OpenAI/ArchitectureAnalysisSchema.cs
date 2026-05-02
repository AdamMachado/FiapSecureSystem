namespace ProcessingService.Infrastructure.AI.OpenAI;

internal static class ArchitectureAnalysisSchema
{
    public const string Name = "architecture_analysis_result";

    // JSON schema for OpenAI Structured Outputs. Keep this stable to maximize prompt-cache hit rate.
    public const string Json = """
    {
      "type": "object",
      "properties": {
        "components": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "id": {
                "type": "string"
              },
              "name": {
                "type": "string"
              },
              "type": {
                "type": "string",
                "enum": [
                  "Unknown",
                  "Actor",
                  "Client",
                  "Frontend",
                  "Backend",
                  "ApiGateway",
                  "Database",
                  "Queue",
                  "Cache",
                  "ExternalService",
                  "Storage",
                  "Network",
                  "Security",
                  "Observability",
                  "Infrastructure"
                ]
              },
              "description": {
                "type": ["string", "null"]
              },
              "tags": {
                "type": "array",
                "items": {
                  "type": "string"
                }
              },
              "connectedTo": {
                "type": "array",
                "items": {
                  "type": "string"
                }
              },
              "metadata": {
                "type": "array",
                "items": {
                  "type": "object",
                  "properties": {
                    "key": {
                      "type": "string"
                    },
                    "value": {
                      "type": "string"
                    }
                  },
                  "required": ["key", "value"],
                  "additionalProperties": false
                }
              }
            },
            "required": [
              "id",
              "name",
              "type",
              "description",
              "tags",
              "connectedTo",
              "metadata"
            ],
            "additionalProperties": false
          }
        },
        "risks": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "id": {
                "type": "string"
              },
              "title": {
                "type": "string"
              },
              "description": {
                "type": "string"
              },
              "severity": {
                "type": "string",
                "enum": ["Low", "Medium", "High", "Critical"]
              },
              "affectedComponentId": {
                "type": ["string", "null"]
              },
              "affectedComponentName": {
                "type": ["string", "null"]
              },
              "impact": {
                "type": ["string", "null"]
              },
              "likelihood": {
                "type": ["string", "null"]
              },
              "evidence": {
                "type": "array",
                "items": {
                  "type": "string"
                }
              }
            },
            "required": [
              "id",
              "title",
              "description",
              "severity",
              "affectedComponentId",
              "affectedComponentName",
              "impact",
              "likelihood",
              "evidence"
            ],
            "additionalProperties": false
          }
        },
        "recommendations": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "id": {
                "type": "string"
              },
              "title": {
                "type": "string"
              },
              "description": {
                "type": "string"
              },
              "category": {
                "type": "string",
                "enum": [
                  "Security",
                  "Scalability",
                  "Reliability",
                  "Performance",
                  "Maintainability",
                  "Observability",
                  "Cost",
                  "Architecture"
                ]
              },
              "priority": {
                "type": "string",
                "enum": ["Low", "Medium", "High", "Critical"]
              },
              "relatedRiskId": {
                "type": ["string", "null"]
              },
              "targetComponentId": {
                "type": ["string", "null"]
              },
              "expectedBenefits": {
                "type": "array",
                "items": {
                  "type": "string"
                }
              }
            },
            "required": [
              "id",
              "title",
              "description",
              "category",
              "priority",
              "relatedRiskId",
              "targetComponentId",
              "expectedBenefits"
            ],
            "additionalProperties": false
          }
        },
        "extractedText": {
          "type": "string"
        },
        "overview": {
          "type": "string"
        },
        "requiresManualReview": {
          "type": "boolean"
        },
        "warnings": {
          "type": "array",
          "items": {
            "type": "string"
          }
        }
      },
      "required": [
        "components",
        "risks",
        "recommendations",
        "extractedText",
        "overview",
        "requiresManualReview",
        "warnings"
      ],
      "additionalProperties": false
    }   
    """;
}
