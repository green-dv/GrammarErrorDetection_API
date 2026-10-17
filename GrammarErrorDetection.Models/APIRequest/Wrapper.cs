using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Models.APIRequest
{
    public class Wrapper<T>
    {
        public T? Data { get; set; }
        public int? Count { get; set; }
        public APIError? Error { get; set; }
    }
}
