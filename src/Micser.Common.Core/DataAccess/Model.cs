using System.ComponentModel.DataAnnotations;

namespace Micser.Common.DataAccess
{
    /// <summary>
    /// Base class for persisted model objects.
    /// </summary>
    public class Model : IIdentifiable
    {
        /// <summary>
        /// Gets or sets the ID key property.
        /// </summary>
        [Key]
        public long Id { get; set; }
    }
}