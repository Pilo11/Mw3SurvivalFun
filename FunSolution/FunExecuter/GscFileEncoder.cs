using System;
using System.IO;
using System.Text;

namespace FunExecuter
{
    internal static class GscFileEncoder
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        internal static byte[] ReadAndEncode(string path)
        {
            return Encode(File.ReadAllBytes(path));
        }

        internal static byte[] Encode(byte[] raw)
        {
            var text = DecodeSource(raw);
            text = NormalizeGscText(text);
            return Utf8NoBom.GetBytes(text);
        }

        private static string DecodeSource(byte[] raw)
        {
            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
                return Utf8NoBom.GetString(raw, 3, raw.Length - 3);

            if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE)
                return Encoding.Unicode.GetString(raw, 2, raw.Length - 2);

            if (raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(raw, 2, raw.Length - 2);

            return Utf8NoBom.GetString(raw);
        }

        private static string NormalizeGscText(string text)
        {
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            text = text.Replace("\uFEFF", string.Empty);

            if (!text.EndsWith("\n", StringComparison.Ordinal))
                text += "\n";

            return text;
        }
    }
}
