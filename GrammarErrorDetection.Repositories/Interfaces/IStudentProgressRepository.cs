using GrammarErrorDetection.Models.Grammar.Reports;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IStudentProgressRepository
{
    Task<List<StudentProgressDto>> List(Guid? classId, Guid? userId);
}
