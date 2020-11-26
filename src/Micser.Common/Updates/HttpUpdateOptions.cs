namespace Micser.Common.Updates
{
    /// <summary>
    /// Contains settings for the <see cref="HttpUpdateService"/>.
    /// </summary>
    public class HttpUpdateOptions
    {
        /// <summary>
        /// Gets or sets the URL of the update manifest.
        /// </summary>
        public string ManifestUrl { get; set; }
    }
}