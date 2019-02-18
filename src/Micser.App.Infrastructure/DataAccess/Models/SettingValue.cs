using Micser.Common.DataAccess;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micser.App.Infrastructure.DataAccess.Models
{
    public class SettingValue : Model
    {
        [Index(IsUnique = true)]
        public string Key { get; set; }

        public string ValueJson { get; set; }

        public string ValueType { get; set; }
    }
}