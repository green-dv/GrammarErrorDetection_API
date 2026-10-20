using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Services;

internal static class ManualClassEligibility
{
    internal sealed record Candidate(EligibleStudentDto Dto,
        RegistrationEntity? StatusSource, RegistrationEntity? RegistrationToReplace);

    internal static List<Candidate> Build(EligibleStudentList filter, IReadOnlyCollection<UserEntity> students,
        IReadOnlyCollection<ClassEntity> courses, IReadOnlyCollection<ModuleNameEntity> modules,
        IReadOnlyCollection<RegistrationEntity> registrations, IReadOnlyCollection<EvaluationEntity> evaluations)
    {
        var classes = courses.ToDictionary(c => c.Id);
        var moduleMap = modules.ToDictionary(m => m.Id);
        var incoming = modules.Where(m => m.NextModule.HasValue).Select(m => m.NextModule!.Value).ToHashSet();
        var grades = evaluations.ToLookup(e => e.RegistrationId);
        var byStudent = registrations.Where(r => r.Active == true).ToLookup(r => r.UserId);
        var results = new List<Candidate>();
        foreach (var student in students.Where(s => s.Active).OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.Id))
        {
            if (!Matches(student.FirstName, filter.FirstName) || !Matches(student.LastName, filter.LastName)
                || !Matches(student.CI, filter.CI)) continue;
            var history = byStudent[student.Id].Where(r => ReservesSeat(r)
                && r.ClassId.HasValue && classes.ContainsKey(r.ClassId.Value)).ToList();
            var assigned = history.Where(r => IsTarget(classes[r.ClassId!.Value], filter))
                .OrderByDescending(r => r.RegistrationDate).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToList();
            var previous = history.Where(r => IsBefore(classes[r.ClassId!.Value], filter))
                .OrderByDescending(r => classes[r.ClassId!.Value].Year)
                .ThenByDescending(r => classes[r.ClassId!.Value].Bimester)
                .ThenByDescending(r => r.RegistrationDate).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
            var sameModule = assigned.FirstOrDefault(r => classes[r.ClassId!.Value].ModuleNameId == filter.ModuleNameId);
            string? reason = sameModule is not null ? "AlreadyAssigned" : null;
            decimal? score = null;
            if (reason is null && previous is not null)
            {
                var priorModuleId = classes[previous.ClassId!.Value].ModuleNameId;
                score = RegistrationEvaluationCalculator.Calculate(grades[previous.Id]).FinalScore;
                if (score >= 70m && moduleMap.TryGetValue(priorModuleId, out var priorModule)
                    && priorModule.NextModule == filter.ModuleNameId) reason = "PassedPreviousModule";
                else if (score < 70m && priorModuleId == filter.ModuleNameId) reason = "RepeatModule";
            }
            // Un módulo de entrada no tiene otro módulo que lo señale como NextModule.
            if (reason is null && previous is null && assigned.Count == 0
                && history.Count == 0 && !incoming.Contains(filter.ModuleNameId)) reason = "NewStudent";
            if (reason is null || (filter.IsAssigned.HasValue && filter.IsAssigned != (assigned.Count > 0))) continue;

            var unassigned = byStudent[student.Id].Where(r => r.ClassId is null)
                .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
            // Un solo destino previo permite un traslado inequívoco. Si hay varios,
            // se muestran todos, pero el servicio exige resolver la ambigüedad.
            var replacement = assigned.Count == 1 ? assigned[0] : assigned.Count == 0 ? unassigned : null;
            var source = replacement ?? sameModule ?? previous;
            results.Add(new Candidate(new EligibleStudentDto
            {
                UserId = student.Id, FirstName = student.FirstName, LastName = student.LastName,
                CI = student.CI, Email = student.Email, Phone = student.Phone,
                EligibilityReason = reason, LastFinalScore = score, IsAssigned = assigned.Count > 0,
                RegistrationStatus = source?.Status ?? "Pending",
                Assignments = assigned.Select(r =>
                {
                    var course = classes[r.ClassId!.Value];
                    return new StudentAssignmentDto
                    {
                        RegistrationId = r.Id, ClassId = course.Id, ModuleNameId = course.ModuleNameId,
                        ModuleName = moduleMap.GetValueOrDefault(course.ModuleNameId)?.Name ?? string.Empty,
                        Parallel = course.Parallel, StartTime = course.StartTime, EndTime = course.EndTime,
                        RegistrationStatus = r.Status
                    };
                }).ToList()
            }, source, replacement));
        }
        // Prioriza a quienes ya están en el módulo y periodo solicitados antes
        // de paginar. OrderByDescending conserva el orden alfabético de cada grupo.
        return results.OrderByDescending(c => c.Dto.EligibilityReason == "AlreadyAssigned").ToList();
    }

    internal static bool ReservesSeat(RegistrationEntity r) =>
        !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
    private static bool Matches(string text, string? query) => string.IsNullOrWhiteSpace(query)
        || text.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    private static bool IsTarget(ClassEntity c, EligibleStudentList f) => c.Year == f.Year && c.Bimester == f.Bimester;
    private static bool IsBefore(ClassEntity c, EligibleStudentList f) => c.Year < f.Year || (c.Year == f.Year && c.Bimester < f.Bimester);
}
