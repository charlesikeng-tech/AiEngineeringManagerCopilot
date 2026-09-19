namespace AiEngineeringManagerCopilot.Infrastructure.AI;

public static class LlmAnalysisSchema
{
    public static BinaryData Create()
    {
        return BinaryData.FromString(
            """
            {
              "type": "object",
              "properties": {
                "summary": {
                  "type": "string"
                },
                "insights": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "category": {
                        "type": "string"
                      },
                      "title": {
                        "type": "string"
                      },
                      "description": {
                        "type": "string"
                      },
                      "impact": {
                        "type": "string"
                      },
                      "recommendation": {
                        "type": "string"
                      }
                    },
                    "required": [
                      "category",
                      "title",
                      "description",
                      "impact",
                      "recommendation"
                    ],
                    "additionalProperties": false
                  }
                },
                "actions": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "title": {
                        "type": "string"
                      },
                      "description": {
                        "type": "string"
                      },
                      "priority": {
                        "type": "string",
                        "enum": [
                          "Low",
                          "Medium",
                          "High",
                          "Critical"
                        ]
                      }
                    },
                    "required": [
                      "title",
                      "description",
                      "priority"
                    ],
                    "additionalProperties": false
                  }
                }
              },
              "required": [
                "summary",
                "insights",
                "actions"
              ],
              "additionalProperties": false
            }
            """);
    }
}