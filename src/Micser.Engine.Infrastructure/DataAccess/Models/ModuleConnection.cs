using Micser.Common.DataAccess;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micser.Engine.Infrastructure.DataAccess.Models
{
    public class ModuleConnection : Model
    {
        public virtual Module SourceModule { get; set; }

        [ForeignKey(nameof(SourceModule))]
        public long SourceModuleId { get; set; }

        public virtual Module TargetModule { get; set; }

        [ForeignKey(nameof(TargetModule))]
        public long TargetModuleId { get; set; }
    }
}