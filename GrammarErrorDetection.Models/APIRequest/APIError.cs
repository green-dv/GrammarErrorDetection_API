using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Models.APIRequest
{
    public class APIError
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorDetails { get; set; } = string.Empty;
    }
}
