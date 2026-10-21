using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ITextRepository
{
    Task<List<TextListItemDto>> List(PaginationDto<TextList> request);
    Task<TextEntity> Insert(TextInsert request);
    Task<TextEntity> Update(TextUpdate request);
}
