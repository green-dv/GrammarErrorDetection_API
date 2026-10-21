using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;
using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface ITextService
    {
        Task<List<TextListItemDto>> List(Guid userId, bool canViewAll, PaginationDto<TextList> request);
        Task<TextListItemDto> Upload(Guid userId, TextUploadRequest request, CancellationToken cancellationToken = default);

    }
}
