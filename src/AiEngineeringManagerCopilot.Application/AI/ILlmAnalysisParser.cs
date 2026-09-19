namespace AiEngineeringManagerCopilot.Application.AI;

public interface ILlmAnalysisParser
{
    LlmAnalysisResult Parse(string content);
}