using GrammarErrorDetection.Models.Nlp;

namespace GrammarErrorDetection.Services.Interfaces;

public interface INlpService
{
    Task<List<NlpSentenceResult>> Analyze(IReadOnlyList<NlpSentenceRequest> sentences, CancellationToken cancellationToken = default);
}
