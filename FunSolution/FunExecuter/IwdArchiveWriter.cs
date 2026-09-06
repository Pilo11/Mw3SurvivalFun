using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FunExecuter
{
    /// <summary>
    /// Writes Call of Duty IWD archives. An IWD is a PKZIP file with STORE
    /// compression, DOS timestamps, ASCII paths, and no Zip64/extra fields.
    /// </summary>
    internal static class IwdArchiveWriter
    {
        private const uint LocalHeaderSignature = 0x04034B50;
        private const uint CentralHeaderSignature = 0x02014B50;
        private const uint EndOfCentralDirSignature = 0x06054B50;
        private const ushort ZipVersion = 20;
        private const ushort StoreMethod = 0;
        private const ushort ArchiveAttribute = 0x20;

        internal static void Write(string outputPath, IReadOnlyList<IwdEntry> entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

            var central = new List<CentralRecord>(entries.Count);
            var now = DateTime.Now;
            var dosTime = ToDosTime(now);
            var dosDate = ToDosDate(now);

            foreach (var entry in entries)
            {
                var nameBytes = Encoding.ASCII.GetBytes(NormalizeZipPath(entry.Path));
                var data = entry.Data ?? Array.Empty<byte>();
                var crc = Crc32.Compute(data);
                var localOffset = (uint)stream.Position;

                writer.Write(LocalHeaderSignature);
                writer.Write(ZipVersion);
                writer.Write((ushort)0);
                writer.Write(StoreMethod);
                writer.Write(dosTime);
                writer.Write(dosDate);
                writer.Write(crc);
                writer.Write((uint)data.Length);
                writer.Write((uint)data.Length);
                writer.Write((ushort)nameBytes.Length);
                writer.Write((ushort)0);
                writer.Write(nameBytes);
                writer.Write(data);

                central.Add(new CentralRecord
                {
                    NameBytes = nameBytes,
                    Crc = crc,
                    Size = (uint)data.Length,
                    LocalOffset = localOffset,
                    DosTime = dosTime,
                    DosDate = dosDate
                });
            }

            var centralStart = (uint)stream.Position;
            foreach (var record in central)
            {
                writer.Write(CentralHeaderSignature);
                writer.Write(ZipVersion);
                writer.Write(ZipVersion);
                writer.Write((ushort)0);
                writer.Write(StoreMethod);
                writer.Write(record.DosTime);
                writer.Write(record.DosDate);
                writer.Write(record.Crc);
                writer.Write(record.Size);
                writer.Write(record.Size);
                writer.Write((ushort)record.NameBytes.Length);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((uint)ArchiveAttribute);
                writer.Write(record.LocalOffset);
                writer.Write(record.NameBytes);
            }

            var centralSize = (uint)stream.Position - centralStart;
            writer.Write(EndOfCentralDirSignature);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)central.Count);
            writer.Write((ushort)central.Count);
            writer.Write(centralSize);
            writer.Write(centralStart);
            writer.Write((ushort)0);
        }

        private static string NormalizeZipPath(string path)
        {
            var normalized = path.Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(normalized) || normalized.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new InvalidOperationException("Invalid IWD entry path: " + path);
            return normalized;
        }

        private static ushort ToDosTime(DateTime value)
        {
            return (ushort)((value.Hour << 11) | (value.Minute << 5) | (value.Second / 2));
        }

        private static ushort ToDosDate(DateTime value)
        {
            return (ushort)(((value.Year - 1980) << 9) | (value.Month << 5) | value.Day);
        }

        private sealed class CentralRecord
        {
            public byte[] NameBytes;
            public uint Crc;
            public uint Size;
            public uint LocalOffset;
            public ushort DosTime;
            public ushort DosDate;
        }
    }

    internal sealed class IwdEntry
    {
        public string Path { get; init; }
        public byte[] Data { get; init; }
    }

    internal static class Crc32
    {
        private static readonly uint[] Table = CreateTable();

        internal static uint Compute(byte[] data)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < data.Length; i++)
                crc = Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] CreateTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int j = 0; j < 8; j++)
                    c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
                table[i] = c;
            }
            return table;
        }
    }
}
