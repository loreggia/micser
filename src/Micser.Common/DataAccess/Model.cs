using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
    }
}