using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Micser.Common;

namespace Micser.DataAccess.Entities
{
    /// <summary>
    /// Base class for persisted model objects.
    /// </summary>
    public class Entity : IIdentifiable
    {
        /// <summary>
        /// Gets or sets the ID key property.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
    }
}