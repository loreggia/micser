using Micser.Common.DataAccess;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micser.Engine.Infrastructure.DataAccess.Models
{
    public class ModuleConnection : Model
    {
        public virtual Module SourceModule { get; set; }

        [ForeignKey(nameof(SourceModule))]
        public int SourceModuleId { get; set; }

        public virtual Module TargetModule { get; set; }

        [ForeignKey(nameof(TargetModule))]
        public int TargetModuleId { get; set; }
    }
}