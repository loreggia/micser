using System;

namespace Micser.Common.Updates
{
    /// <summary>
    /// Contains information about an application update package.
    /// </summary>
    public class UpdateManifest
    {
        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the description (i.e. change log).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the file name of the update package.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the version string.
        /// </summary>
        public string? Version { get; set; }

        public override string ToString()
        {
            return $"Version: {Version}, FileName: {FileName}, Date: {Date}";
        }
    }
}