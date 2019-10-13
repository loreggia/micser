using Micser.Common.Settings;
using ProtoBuf;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Micser.Common.Api
{
    [ProtoContract]
    public class SettingValueSurrogate
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2, DynamicType = true)]
        public object ValueObject { get; set; }

        [ProtoMember(3)]
        public byte[] ValuePrimitive { get; set; }

        public static implicit operator SettingValueDto(SettingValueSurrogate surrogate)
        {
            if (surrogate == null)
            {
                return null;
            }

            var result = new SettingValueDto { Key = surrogate.Key };

            result.Value = surrogate.ValuePrimitive?.Length > 0
                ? Deserialize(surrogate.ValuePrimitive)
                : surrogate.ValueObject;

            return result;
        }

        public static implicit operator SettingValueSurrogate(SettingValueDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            var result = new SettingValueSurrogate { Key = dto.Key };

            if (dto.Value != null)
            {
                var type = dto.Value.GetType();
                var contractAttribute = type.GetCustomAttribute<ProtoContractAttribute>();
                if (type.IsPrimitive || contractAttribute == null)
                {
                    result.ValuePrimitive = Serialize(dto.Value);
                }
                else
                {
                    result.ValueObject = dto.Value;
                }
            }

            return result;
        }

        private static object Deserialize(byte[] b)
        {
            if (b == null)
            {
                return null;
            }

            using var ms = new MemoryStream(b);
            var formatter = new BinaryFormatter();
            return formatter.Deserialize(ms);
        }

        private static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            using var ms = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, o);
            return ms.ToArray();
        }
    }
}