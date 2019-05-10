using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Micser.Common.Api
{
    /// <summary>
    /// A JSON converter that handles the serialization of a <see cref="JsonMessage"/>'s content.
    /// </summary>
    public class JsonMessageConverter : JsonConverter<JsonMessage>
    {
        /// <summary>
        /// Always false.
        /// </summary>
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override JsonMessage ReadJson(JsonReader reader, Type objectType, JsonMessage existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var result = hasExistingValue ? existingValue : (JsonMessage)Activator.CreateInstance(objectType);
            var jObject = JObject.Load(reader);

            var oProperties = objectType.GetProperties();

            foreach (var propertyInfo in oProperties)
            {
                if (propertyInfo.Name == nameof(JsonMessage.Content) || !propertyInfo.CanWrite)
                {
                    continue;
                }

                if (jObject.TryGetValue(propertyInfo.Name, out var token))
                {
                    propertyInfo.SetValue(result, token.ToObject(propertyInfo.PropertyType));
                }
            }

            if (jObject.TryGetValue(nameof(JsonMessage.ContentType), out var contentType) &&
                jObject.TryGetValue(nameof(JsonMessage.Content), out var content))
            {
                var typeName = contentType.Value<string>();

                if (typeName != null)
                {
                    var type = Type.GetType(typeName);

                    if (type != null)
                    {
                        result.Content = content.ToObject(type, serializer);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override void WriteJson(JsonWriter writer, JsonMessage value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}