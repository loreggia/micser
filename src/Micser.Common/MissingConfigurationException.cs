using System;

#pragma warning disable RCS1194

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

#pragma warning restore RCS1194
