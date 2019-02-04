using System.ComponentModel.DataAnnotations;

namespace Micser.Common.DataAccess
{
    public class Model : IIdentifiable
    {
        [Key]
        public long Id { get; set; }
    }
}