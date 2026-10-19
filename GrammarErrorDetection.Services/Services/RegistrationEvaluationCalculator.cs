using GrammarErrorDetection.Models.Grammar.Evaluations;

namespace GrammarErrorDetection.Services.Services;

internal static class RegistrationEvaluationCalculator
{
    internal sealed record Result(decimal? FinalScore, string? Reason);

    internal static Result Calculate(IEnumerable<EvaluationEntity> evaluations)
    {
        var active = evaluations.Where(e => e.Active).ToList();
        if (active.Count != 2 || active.Count(e => e.Period == 1) != 1 || active.Count(e => e.Period == 2) != 1)
            return new(null, "Se requiere exactamente una evaluación activa del periodo 1 y una del periodo 2.");

        var totals = new List<int>();
        foreach (var evaluation in active)
        {
            if (evaluation.WritingScore is null || evaluation.OralScore is null
                || evaluation.ListeningScore is null || evaluation.ReadingScore is null
                || evaluation.GrammarScore is null || evaluation.VocabularyScore is null
                || evaluation.AcademicPerformance is null)
                return new(null, $"La evaluación del periodo {evaluation.Period} tiene componentes sin calificar.");

            if (!Within(evaluation.WritingScore.Value, 30) || !Within(evaluation.OralScore.Value, 15)
                || !Within(evaluation.ListeningScore.Value, 10) || !Within(evaluation.ReadingScore.Value, 10)
                || !Within(evaluation.GrammarScore.Value, 10) || !Within(evaluation.VocabularyScore.Value, 10)
                || evaluation.GrammarScore.Value + evaluation.VocabularyScore.Value > 10
                || !Within(evaluation.AcademicPerformance.Value, 25))
                return new(null, $"La evaluación del periodo {evaluation.Period} tiene notas fuera de rango; Grammar y Vocabulary juntos admiten hasta 10 puntos.");

            totals.Add(evaluation.WritingScore.Value + evaluation.OralScore.Value
                + evaluation.ListeningScore.Value + evaluation.ReadingScore.Value
                + evaluation.GrammarScore.Value + evaluation.VocabularyScore.Value
                + evaluation.AcademicPerformance.Value);
        }

        // Los componentes ya son puntos ponderados. No se aplican porcentajes otra vez.
        return new((totals[0] + totals[1]) / 2m, null);
    }

    private static bool Within(int score, int maximum) => score >= 0 && score <= maximum;
}
