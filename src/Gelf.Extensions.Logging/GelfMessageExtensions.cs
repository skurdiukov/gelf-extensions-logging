using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gelf.Extensions.Logging
{
    public static class GelfMessageExtensions
    {
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

        private static JObject GetJObject(GelfMessage message)
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

        public static string ToJsonString(this GelfMessage message)
        {
            return GetJObject(message).ToString(Formatting.None);
        }

        public static Stream ToJsonStream(this GelfMessage message)
        {
            var stream = new MemoryStream();

            try
            {
                using (var streamWriter = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true))
                using (var jsonWriter = new JsonTextWriter(streamWriter) {Formatting = Formatting.None})
                {
                    GetJObject(message).WriteTo(jsonWriter);
                    jsonWriter.Flush();

                    stream.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception)
            {
                stream.Dispose();
                throw;
            }

            return stream;
        }
    }
}
