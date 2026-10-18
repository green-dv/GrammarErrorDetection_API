using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;

namespace GrammarErrorDetection.Repositories.Utils
{
    internal static class RepositoryValidation
    {
        internal static void ValidateInput(object request)
        {
            ArgumentNullException.ThrowIfNull(request);
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        }

        internal static void ValidatePagination<T>(PaginationDto<T> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
            if (request.Filter is not null)
                ValidateInput(request.Filter);
        }
    }
}
