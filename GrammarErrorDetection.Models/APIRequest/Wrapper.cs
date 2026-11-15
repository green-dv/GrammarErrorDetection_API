using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Models.APIRequest
{
    public class Wrapper<T>
    {
        public T? Data { get; set; }
        public int? Count { get; set; }
        // Listados con el formato de dos DataTables: total de registros que cumplen los filtros (sin paginar).
        public long? TotalCount { get; set; }
        public APIError? Error { get; set; }
    }
}
