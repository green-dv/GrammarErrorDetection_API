using GrammarErrorDetection.Models.Grammar.Dashboards;
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
        async Task<PagedList<TextListItemDto>> ListPage(Guid userId, bool canViewAll, PaginationDto<TextList> request)
        {
            var items = await List(userId, canViewAll, request);
            return new PagedList<TextListItemDto>(items, items.Count);
        }
        Task<TextListItemDto> Upload(Guid userId, TextUploadRequest request, CancellationToken cancellationToken = default);

    }
}
