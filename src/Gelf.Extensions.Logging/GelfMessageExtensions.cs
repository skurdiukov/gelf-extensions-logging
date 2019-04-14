using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gelf.Extensions.Logging
{
    public static class GelfMessageExtensions
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);

        private static bool IsNumeric(object value)
        {
            return value is sbyte
                || value is byte
                || value is short
                || value is ushort
                || value is int
                || value is uint
                || value is long
                || value is ulong
                || value is float
                || value is double
                || value is decimal;
        }

        public static JObject ToJObject(this GelfMessage message)
        {
            var messageJson = JObject.FromObject(message, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });

            foreach (var field in message.AdditionalFields)
            {
                if (IsNumeric(field.Value))
                {
                    messageJson[$"_{field.Key}"] = JToken.FromObject(field.Value);
                }
                else
                {
                    messageJson[$"_{field.Key}"] = field.Value?.ToString();
                }
            }

            return messageJson;
        }

        public static string ToJson(this GelfMessage message)
        {
            return message.ToJObject().ToString(Formatting.None);
        }

        public static async Task WriteToStreamAsync(this GelfMessage message, Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream, Utf8NoBom, 1024, false))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                await message.ToJObject().WriteToAsync(jsonWriter);
            }
        }
    }
}
