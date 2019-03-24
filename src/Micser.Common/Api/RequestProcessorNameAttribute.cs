using System;

namespace Micser.Common.Api
{
    public class RequestProcessorNameAttribute : Attribute
    {
        public const string DefaultName = "<default>";

        public RequestProcessorNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}