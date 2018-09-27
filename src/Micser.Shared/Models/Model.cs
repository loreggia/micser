using LiteDB;

namespace Micser.Shared.Models
{
    public class Model
    {
        [BsonId(true)]
        public int Id { get; set; }
    }
}