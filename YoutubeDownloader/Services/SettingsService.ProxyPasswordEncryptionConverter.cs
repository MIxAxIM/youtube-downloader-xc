using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoutubeDownloader.Services;

public partial class SettingsService
{
    private class ProxyPasswordEncryptionConverter : JsonConverter<string?>
    {
        public override string? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                return null;

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                return Encoding.UTF8.GetString(SettingsProtection.DecryptFromHex(value));
            }
            catch (Exception ex)
                when (ex
                        is FormatException
                            or CryptographicException
                            or ArgumentException
                            or IndexOutOfRangeException
                            or DecoderFallbackException
                )
            {
                return null;
            }
        }

        public override void Write(
            Utf8JsonWriter writer,
            string? value,
            JsonSerializerOptions options
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(SettingsProtection.EncryptToHex(Encoding.UTF8.GetBytes(value)));
        }
    }
}
