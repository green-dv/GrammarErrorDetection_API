using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Models.APIRequest
{
    public class PaginationDto<T>
    {
        public T? Filter { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
