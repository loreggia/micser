using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.IO;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public class MessagePackMessageSerializer : IMessageSerializer
    {
        static MessagePackMessageSerializer()
        {
            CompositeResolver.RegisterAndSetAsDefault(
                new IMessagePackFormatter[] { TypelessFormatter.Instance },
                new[] { ContractlessStandardResolver.Instance });
        }

        public async Task<T> DeserializeAsync<T>(Stream stream)
        {
            return await MessagePackSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
        }

        public async Task SerializeAsync<T>(Stream stream, T value)
        {
            await MessagePackSerializer.SerializeAsync(stream, value).ConfigureAwait(false);
        }
    }
}