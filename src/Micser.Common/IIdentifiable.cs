namespace Micser.Common
{
    /// <summary>
    /// Describes an object with an ID.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets or sets the object's ID.
        /// </summary>
        long Id { get; set; }
    }
}