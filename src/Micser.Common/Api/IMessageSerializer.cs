using System.IO;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public interface IMessageSerializer
    {
        Task<T> DeserializeAsync<T>(Stream stream);

        Task SerializeAsync<T>(Stream stream, T value);
    }
}