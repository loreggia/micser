using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure.Api
{
    public class ErrorList
    {
        public ErrorList()
        {
        }

        public ErrorList(params string[] errors)
        {
            Errors = errors.Select(e => new Error
            {
                Message = e
            });
        }

        public ErrorList(params Exception[] exceptions)
        {
            Errors = exceptions.Select(ex => new Error
            {
                Code = ex.HResult.ToString(),
                Message = ex.Message
            });
        }

        public IEnumerable<Error> Errors { get; set; }

        public override string ToString()
        {
            var errors = Errors?.Select(e => $"{{Code: '{e.Code}', Message: '{e.Message}', Field: '{e.Field}'}}") ?? new string[0];
            return $"{string.Join(", ", errors)}";
        }
    }
}