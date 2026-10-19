using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Services;

public sealed partial class RegistrationService
{
    public async Task<RegistrationGenerationResult> GenerateNextBimester(RegistrationGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var input = await generation.Load(request);
        var sourceClassIds = input.SourceClasses.Select(c => c.Id).ToHashSet();
        var evaluationGroups = input.Evaluations.ToLookup(e => e.RegistrationId);
        var passed = new HashSet<Guid>();
        var failed = new HashSet<Guid>();
        var missingGrades = new Dictionary<Guid, string>();
        foreach (var registration in input.Registrations.Where(r =>
                     r.ClassId.HasValue && sourceClassIds.Contains(r.ClassId.Value)))
        {
            var grade = RegistrationEvaluationCalculator.Calculate(evaluationGroups[registration.Id]);
            if (grade.FinalScore is null)
                missingGrades[registration.Id] = grade.Reason!;
            else if (grade.FinalScore >= 70m)
                passed.Add(registration.Id);
            else
                failed.Add(registration.Id);
        }

        var result = await generation.Save(request, input, passed, failed);
        foreach (var skipped in result.Skipped)
            if (missingGrades.TryGetValue(skipped.RegistrationId, out var reason))
                skipped.Reason = reason;
        return result;
    }
}
