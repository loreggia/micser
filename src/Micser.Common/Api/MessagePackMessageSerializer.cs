using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.IO;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public class MessagePackMessageSerializer : IMessageSerializer
    {
        private static readonly MessagePackSerializerOptions Options;

        static MessagePackMessageSerializer()
        {
            var resolver = CompositeResolver.Create(
                new[] { TypelessFormatter.Instance },
                new[] { TypelessObjectResolver.Instance, ContractlessStandardResolver.Instance });
            Options = MessagePackSerializerOptions
                    .Standard
                    .WithResolver(resolver);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream)
        {
            try
            {
                return await MessagePackSerializer.DeserializeAsync<T>(stream, Options).ConfigureAwait(false);
            }
            catch (MessagePackSerializationException ex)
            {
                // unwrap exceptions
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }
        }

        public async Task SerializeAsync<T>(Stream stream, T value)
        {
            try
            {
                await MessagePackSerializer.SerializeAsync(stream, value, Options).ConfigureAwait(false);
            }
            catch (MessagePackSerializationException ex)
            {
                // unwrap exceptions
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }
        }
    }
}