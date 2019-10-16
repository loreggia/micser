using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Extensions
{
    public static class PipeStreamExtensions
    {
        public static Task<int> CopyMessageToAsync(this PipeStream pipe, Stream stream)
        {
            return pipe.CopyMessageToAsync(stream, CancellationToken.None);
        }

        public static async Task<int> CopyMessageToAsync(this PipeStream pipe, Stream stream, CancellationToken token)
        {
            const int minBufferSize = 1024;

            var buffer = ArrayPool<byte>.Shared.Rent(minBufferSize);
            var totalCount = 0;

            try
            {
                do
                {
                    var count = await pipe.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    await stream.WriteAsync(buffer, 0, count, token).ConfigureAwait(false);
                    totalCount += count;
                } while (!pipe.IsMessageComplete);

                return totalCount;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}