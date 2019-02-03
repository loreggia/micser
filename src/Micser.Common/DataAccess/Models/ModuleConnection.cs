using System.ComponentModel.DataAnnotations.Schema;

namespace Micser.Common.DataAccess.Models
{
    public class ModuleConnection : Model
    {
        public virtual Module FromModule { get; set; }

        [ForeignKey(nameof(FromModule))]
        public int FromModuleId { get; set; }

        public virtual Module ToModule { get; set; }

        [ForeignKey(nameof(ToModule))]
        public int ToModuleId { get; set; }
    }
}