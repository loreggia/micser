namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Describes an object that is selectable.
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// Gets or sets whether the object is selected.
        /// </summary>
        bool IsSelected { get; set; }
    }
}