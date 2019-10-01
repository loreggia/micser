using System;

namespace Micser.Common
{
    public class MissingConfigurationException : Exception
    {
        public MissingConfigurationException(string section)
        {
            Section = section;
        }

        public string Section { get; }
    }
}