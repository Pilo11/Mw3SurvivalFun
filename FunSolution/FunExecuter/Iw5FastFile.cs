using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FunExecuter
{
    internal sealed class Iw5ScriptSlot
    {
        internal int BufferOffset;
        internal int BufferLength;
        internal int BytecodeOffset;
        internal int BytecodeLength;
        internal int CompressedLenFieldOffset = -1;
        internal int LenFieldOffset = -1;
        internal int BytecodeLenFieldOffset = -1;
        internal byte[] CompressedBuffer;
        internal byte[] Bytecode;
        internal byte[] Stack;
        internal string Name = "";
    }

    internal sealed class Iw5FastFile
    {
        private const int MaxScriptZlibBytes = 512 * 1024;
        internal const int ScriptBlockIndex = 8;

        internal byte[] Prefix;
        internal byte[] Payload;

        internal static Iw5FastFile Load(string path)
        {
            var file = File.ReadAllBytes(path);
            if (file.Length < 24)
                throw new InvalidDataException("FastFile is too small: " + path);

            var magic = Encoding.ASCII.GetString(file, 0, 8);
            if (magic == "IWffu100")
                return LoadUnsigned(file);

            if (magic == "IWff0100")
                throw new InvalidDataException(
                    "FastFile is signed IWff0100. Vanilla iw5sp loads unsigned IWffu100 after an OAT-style unwrap; this copy still has the Steam signature wrapper. Restore patch_survival.vanilla.ff or verify the game files and retry.");

            throw new InvalidDataException("Unsupported FastFile magic \"" + magic.Replace('\0', ' ').Trim() + "\".");
        }

        internal void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var compressed = CompressZlib(Payload);
            using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            output.Write(Prefix, 0, Prefix.Length);
            output.Write(compressed, 0, compressed.Length);
        }

        internal int ReplaceSentryEquipmentPrice(int oldPrice, int newPrice)
        {
            var oldText = oldPrice.ToString();
            var newText = PadPrice(newPrice, oldText.Length);
            var packed = Encoding.ASCII.GetBytes("sentry\0equipment\0" + oldText);
            var packedNext = Encoding.ASCII.GetBytes("sentry\0equipment\0" + newText);
            var csv = Encoding.ASCII.GetBytes("sentry,equipment," + oldText);
            var csvNext = Encoding.ASCII.GetBytes("sentry,equipment," + newText);
            var hits = ReplaceBytes(packed, packedNext) + ReplaceBytes(csv, csvNext);
            if (hits == 0)
            {
                hits += ReplacePriceNearToken("sentry", oldText, newText);
                hits += ReplacePriceNearToken("Sentry Gun", oldText, newText);
            }
            return hits;
        }

        private int ReplacePriceNearToken(string token, string oldPrice, string newPrice)
        {
            var sentry = Encoding.ASCII.GetBytes(token);
            var price = Encoding.ASCII.GetBytes(oldPrice);
            var next = Encoding.ASCII.GetBytes(newPrice);
            var count = 0;
            var start = 0;
            while (true)
            {
                var index = Payload.AsSpan(start).IndexOf(sentry);
                if (index < 0)
                    break;

                var abs = start + index;
                var after = abs + sentry.Length;
                if (after < Payload.Length)
                {
                    var c = Payload[after];
                    if (c == '_' || c == 'g' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    {
                        start = abs + sentry.Length;
                        continue;
                    }
                }

                var windowStart = abs;
                var windowEnd = Math.Min(Payload.Length, abs + 96);
                var found = Payload.AsSpan(windowStart, windowEnd - windowStart).IndexOf(price);
                if (found >= 0)
                {
                    var priceAbs = windowStart + found;
                    Buffer.BlockCopy(next, 0, Payload, priceAbs, next.Length);
                    count++;
                    start = priceAbs + price.Length;
                    continue;
                }

                start = abs + sentry.Length;
            }

            return count;
        }

        private static string PadPrice(int price, int width)
        {
            var text = price.ToString();
            if (text.Length > width)
                throw new ArgumentException("Price " + price + " does not fit in " + width + " characters.");
            return text.PadRight(width);
        }

        private int ReplaceBytes(byte[] find, byte[] replace)
        {
            if (find.Length != replace.Length)
                throw new ArgumentException("Byte replacements must keep the same length.");

            var count = 0;
            var start = 0;
            while (true)
            {
                var index = Payload.AsSpan(start).IndexOf(find);
                if (index < 0)
                    break;

                var abs = start + index;
                Buffer.BlockCopy(replace, 0, Payload, abs, replace.Length);
                count++;
                start = abs + find.Length;
            }

            return count;
        }

        internal List<Iw5ScriptSlot> FindScriptSlots()
        {
            var slots = new List<Iw5ScriptSlot>();
            var payload = Payload;
            var i = 0;
            while (i < payload.Length - 2)
            {
                if (!IsZlibHeader(payload, i))
                {
                    i++;
                    continue;
                }

                var cap = Math.Min(MaxScriptZlibBytes, payload.Length - i);
                if (!TryInflateLength(payload, i, cap, out var stack) || stack.Length < 32 || !LooksLikeGscStack(stack))
                {
                    i++;
                    continue;
                }

                var zlibLen = FindExactZlibLength(payload, i, cap, stack.Length);
                if (zlibLen < 8)
                {
                    i++;
                    continue;
                }

                if (TryReadSlot(payload, i, zlibLen, stack, out var slot))
                {
                    LogFoundSlot(slot);
                    slots.Add(slot);
                    i += Math.Max(slot.BufferLength, 2);
                    continue;
                }

                if (IsInterestingStack(stack))
                {
                    Console.WriteLine(
                        "Found GSC stack at offset " + i + " (" + stack.Length +
                        " bytes, zlib " + zlibLen + ") without a matching ScriptFile header." +
                        StackMarkerSuffix(stack));
                    LogIntsBefore(payload, i, 64);
                }

                i += Math.Max(zlibLen, 2);
            }

            return slots;
        }

        internal bool TryReplaceScript(Iw5ScriptSlot slot, byte[] compressedBuffer, int stackLen, byte[] bytecode, IReadOnlyList<Iw5ScriptSlot> allSlots)
        {
            compressedBuffer = GscBinFile.SmallestCompress(compressedBuffer, stackLen);

            if (compressedBuffer.Length <= slot.BufferLength && bytecode.Length <= slot.BytecodeLength)
            {
                Array.Clear(Payload, slot.BufferOffset, slot.BufferLength);
                Buffer.BlockCopy(compressedBuffer, 0, Payload, slot.BufferOffset, compressedBuffer.Length);
                Array.Clear(Payload, slot.BytecodeOffset, slot.BytecodeLength);
                Buffer.BlockCopy(bytecode, 0, Payload, slot.BytecodeOffset, bytecode.Length);
                WriteInt(slot.LenFieldOffset, stackLen);
                return true;
            }

            var newBytecodeOffset = slot.BufferOffset + compressedBuffer.Length;
            var newEnd = newBytecodeOffset + bytecode.Length;
            var originalEnd = Math.Max(slot.BufferOffset + slot.BufferLength, slot.BytecodeOffset + slot.BytecodeLength);
            var extra = newEnd - originalEnd;
            if (extra < 0)
                extra = 0;

            if (extra > 0)
            {
                InsertBytes(originalEnd, extra);
                GrowBlock(ScriptBlockIndex, extra);
                Console.WriteLine("Inserted " + extra + " bytes into the FastFile stream after " + SlotLabel(slot) + " and grew XFILE_BLOCK_SCRIPT.");
            }

            Buffer.BlockCopy(compressedBuffer, 0, Payload, slot.BufferOffset, compressedBuffer.Length);
            Buffer.BlockCopy(bytecode, 0, Payload, newBytecodeOffset, bytecode.Length);
            WriteInt(slot.CompressedLenFieldOffset, compressedBuffer.Length);
            WriteInt(slot.LenFieldOffset, stackLen);
            WriteInt(slot.BytecodeLenFieldOffset, bytecode.Length);
            return true;
        }

        internal void InsertBytes(int index, int count)
        {
            if (count <= 0)
                return;
            var grown = new byte[Payload.Length + count];
            Buffer.BlockCopy(Payload, 0, grown, 0, index);
            Buffer.BlockCopy(Payload, index, grown, index + count, Payload.Length - index);
            Payload = grown;
        }

        internal void GrowBlock(int blockIndex, int extra)
        {
            if (extra <= 0)
                return;

            var size = BitConverter.ToUInt32(Payload, 0);
            BitConverter.GetBytes(size + (uint)extra).CopyTo(Payload, 0);

            var blockSizeOffset = 8 + blockIndex * 4;
            if (blockSizeOffset + 4 > Payload.Length)
                return;

            var blockSize = BitConverter.ToUInt32(Payload, blockSizeOffset);
            BitConverter.GetBytes(blockSize + (uint)extra).CopyTo(Payload, blockSizeOffset);
            Console.WriteLine(
                "XFile.size " + size + " -> " + (size + extra) +
                ", blockSize[" + blockIndex + "] " + blockSize + " -> " + (blockSize + extra) + ".");
        }

        private static string SlotLabel(Iw5ScriptSlot slot)
        {
            return string.IsNullOrEmpty(slot.Name) ? ("@" + slot.BufferOffset) : slot.Name;
        }

        private void WriteInt(int offset, int value)
        {
            if (offset < 0)
                return;
            BitConverter.GetBytes(value).CopyTo(Payload, offset);
        }

        private static Iw5FastFile LoadUnsigned(byte[] file)
        {
            const int prefixLength = 21;
            if (file.Length < prefixLength + 2 || file[prefixLength] != 0x78)
                throw new InvalidDataException("Unsigned FastFile does not have a zlib payload at offset 21.");

            var prefix = new byte[prefixLength];
            Buffer.BlockCopy(file, 0, prefix, 0, prefixLength);
            var zlibLen = file.Length - prefixLength;
            if (!TryInflateLength(file, prefixLength, zlibLen, out var payload))
                throw new InvalidDataException("Failed to inflate unsigned FastFile zlib payload.");

            Console.WriteLine("Inflated FastFile payload: " + payload.Length + " bytes.");
            return new Iw5FastFile { Prefix = prefix, Payload = payload };
        }

        private static bool TryReadSlot(byte[] payload, int bufferOffset, int zlibLen, byte[] stack, out Iw5ScriptSlot slot)
        {
            slot = null;
            var nearby = 8192;
            var beforeStart = Math.Max(0, bufferOffset - nearby);
            if (TryReadSlotInRange(payload, beforeStart, bufferOffset, bufferOffset, zlibLen, stack, out slot))
                return true;

            var afterStart = bufferOffset + zlibLen;
            var afterEnd = Math.Min(payload.Length, afterStart + nearby);
            if (afterStart < payload.Length
                && TryReadSlotInRange(payload, afterStart, afterEnd, bufferOffset, zlibLen, stack, out slot))
                return true;

            if (beforeStart > 0
                && TryReadSlotInRange(payload, 0, beforeStart, bufferOffset, zlibLen, stack, out slot))
                return true;

            if (afterEnd < payload.Length
                && TryReadSlotInRange(payload, afterEnd, payload.Length, bufferOffset, zlibLen, stack, out slot))
                return true;

            return false;
        }

        private static bool TryReadSlotInRange(
            byte[] payload,
            int rangeStart,
            int rangeEnd,
            int bufferOffset,
            int zlibLen,
            byte[] stack,
            out Iw5ScriptSlot slot)
        {
            slot = null;
            var stackLen = stack.Length;
            rangeStart = Math.Max(0, rangeStart);
            for (var field = rangeStart; field + 12 <= rangeEnd; field++)
            {
                if (!TryParseLengthTriple(payload, field, stackLen, zlibLen, out var compressedLen, out var bytecodeLen, out var compressedLenField, out var lenFieldOffset, out var bytecodeLenField))
                    continue;

                if (bufferOffset + compressedLen > payload.Length)
                    continue;

                foreach (var bytecodeOffset in BytecodeCandidates(bufferOffset, compressedLen))
                {
                    if (bytecodeOffset < 0 || bytecodeOffset + bytecodeLen > payload.Length)
                        continue;

                    slot = new Iw5ScriptSlot
                    {
                        BufferOffset = bufferOffset,
                        BufferLength = compressedLen,
                        BytecodeOffset = bytecodeOffset,
                        BytecodeLength = bytecodeLen,
                        CompressedLenFieldOffset = compressedLenField,
                        LenFieldOffset = lenFieldOffset,
                        BytecodeLenFieldOffset = bytecodeLenField,
                        CompressedBuffer = Slice(payload, bufferOffset, compressedLen),
                        Bytecode = Slice(payload, bytecodeOffset, bytecodeLen),
                        Stack = stack,
                        Name = ReadNearbyName(payload, field, Math.Min(bufferOffset, field + 256))
                    };
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseLengthTriple(
            byte[] payload,
            int field,
            int stackLen,
            int zlibLen,
            out int compressedLen,
            out int bytecodeLen,
            out int compressedLenField,
            out int lenFieldOffset,
            out int bytecodeLenField)
        {
            compressedLen = 0;
            bytecodeLen = 0;
            compressedLenField = -1;
            lenFieldOffset = -1;
            bytecodeLenField = -1;
            if (field < 0 || field + 12 > payload.Length)
                return false;

            var a = BitConverter.ToInt32(payload, field);
            var b = BitConverter.ToInt32(payload, field + 4);
            var c = BitConverter.ToInt32(payload, field + 8);

            if (IsTriple(a, b, c, stackLen, zlibLen))
            {
                compressedLen = a;
                bytecodeLen = c;
                compressedLenField = field;
                lenFieldOffset = field + 4;
                bytecodeLenField = field + 8;
                return true;
            }

            if (IsTriple(b, a, c, stackLen, zlibLen))
            {
                compressedLen = b;
                bytecodeLen = c;
                compressedLenField = field + 4;
                lenFieldOffset = field;
                bytecodeLenField = field + 8;
                return true;
            }

            return false;
        }

        private static bool IsTriple(int compressedLen, int len, int bytecodeLen, int stackLen, int zlibLen)
        {
            if (len != stackLen && len != stackLen + 1)
                return false;
            if (bytecodeLen < 16 || bytecodeLen > 2_000_000)
                return false;
            if (compressedLen < 8 || compressedLen > MaxScriptZlibBytes)
                return false;
            return compressedLen >= zlibLen - 16 && compressedLen <= zlibLen + 4096;
        }

        private static IEnumerable<int> BytecodeCandidates(int bufferOffset, int compressedLen)
        {
            var raw = bufferOffset + compressedLen;
            yield return raw;
            foreach (var align in new[] { 4, 8, 16, 32, 64, 128 })
            {
                var aligned = (raw + (align - 1)) & ~(align - 1);
                if (aligned != raw)
                    yield return aligned;
            }
        }

        private static void LogFoundSlot(Iw5ScriptSlot slot)
        {
            var label = string.IsNullOrEmpty(slot.Name) ? ("@" + slot.BufferOffset) : slot.Name;
            Console.WriteLine(
                "ScriptFile " + label + ": stack " + slot.Stack.Length +
                ", zlib " + slot.BufferLength + ", bytecode " + slot.BytecodeLength +
                StackMarkerSuffix(slot.Stack));
        }

        private static bool IsInterestingStack(byte[] stack)
        {
            return StackContains(stack, "wave_started")
                || StackContains(stack, "survival_armories")
                || StackContains(stack, "survival_armory")
                || StackContains(stack, "specops_ui_weaponstore");
        }

        private static string StackMarkerSuffix(byte[] stack)
        {
            var parts = new List<string>();
            if (StackContains(stack, "wave_started"))
                parts.Add("wave_started");
            if (StackContains(stack, "survival_armories"))
                parts.Add("survival_armories");
            if (StackContains(stack, "survival_armory"))
                parts.Add("survival_armory");
            if (StackContains(stack, "sentry_gl"))
                parts.Add("sentry_gl");
            if (StackContains(stack, "specops_ui_weaponstore"))
                parts.Add("weaponstore");
            return parts.Count == 0 ? "" : ", has " + string.Join(" ", parts);
        }

        private static string ReadNearbyName(byte[] payload, int field, int bufferOffset)
        {
            var immediatelyBefore = ReadCStringBefore(payload, bufferOffset);
            if (!string.IsNullOrEmpty(immediatelyBefore) && IsPrintableName(immediatelyBefore))
                return immediatelyBefore;

            foreach (var nameOffset in new[] { field + 32, field + 20, field + 12, field - 4, field + 40 })
            {
                if (nameOffset < 0 || nameOffset >= bufferOffset)
                    continue;
                var end = nameOffset;
                while (end < bufferOffset && payload[end] != 0)
                    end++;
                var length = end - nameOffset;
                if (length < 1 || length > 128)
                    continue;
                var name = Encoding.ASCII.GetString(payload, nameOffset, length);
                if (IsPrintableName(name))
                    return name;
            }

            return "";
        }

        private static string ReadCStringBefore(byte[] payload, int offset)
        {
            var end = offset;
            while (end > 0 && payload[end - 1] == 0)
                end--;
            var start = end;
            while (start > 0 && payload[start - 1] >= 32 && payload[start - 1] < 127)
                start--;
            var length = end - start;
            if (length < 1 || length > 128)
                return "";
            return Encoding.ASCII.GetString(payload, start, length);
        }

        private static bool IsPrintableName(string name)
        {
            foreach (var c in name)
            {
                if (c < 32 || c > 126)
                    return false;
            }
            return name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || char.IsLetterOrDigit(name[0]);
        }

        private static bool LooksLikeGscStack(byte[] data)
        {
            var nulls = 0;
            var printable = 0;
            foreach (var b in data)
            {
                if (b == 0)
                    nulls++;
                else if (b >= 32 && b < 127)
                    printable++;
            }

            return nulls >= 4 && printable > data.Length / 3;
        }

        internal static bool StackContains(byte[] stack, string text)
        {
            return stack.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;
        }

        private static void LogIntsBefore(byte[] payload, int offset, int bytes)
        {
            var start = Math.Max(0, offset - bytes);
            var parts = new List<string>();
            for (var p = start; p + 4 <= offset; p += 4)
                parts.Add(BitConverter.ToInt32(payload, p).ToString());
            Console.WriteLine("  ints before offset: " + string.Join(", ", parts));
        }

        private static bool IsZlibHeader(byte[] data, int offset)
        {
            if (data[offset] != 0x78)
                return false;
            var check = (data[offset] << 8) | data[offset + 1];
            return check % 31 == 0;
        }

        private static int FindExactZlibLength(byte[] data, int offset, int maxLength, int expectedOutputLen)
        {
            var lo = 8;
            var hi = maxLength;
            var best = -1;
            while (lo <= hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (TryInflateLength(data, offset, mid, out var output) && output.Length == expectedOutputLen)
                {
                    best = mid;
                    hi = mid - 1;
                }
                else
                {
                    lo = mid + 1;
                }
            }

            return best;
        }

        private static bool TryInflateLength(byte[] data, int offset, int length, out byte[] output)
        {
            output = null;
            if (offset < 0 || length < 4 || offset + length > data.Length)
                return false;

            try
            {
                using var input = new MemoryStream(data, offset, length, writable: false);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                using var inflated = new MemoryStream();
                zlib.CopyTo(inflated);
                if (inflated.Length < 1)
                    return false;
                output = inflated.ToArray();
                return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static byte[] CompressZlib(byte[] data)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(data, 0, data.Length);
            return output.ToArray();
        }

        private static byte[] Slice(byte[] data, int offset, int length)
        {
            var copy = new byte[length];
            Buffer.BlockCopy(data, offset, copy, 0, length);
            return copy;
        }
    }

    internal static class GscBinFile
    {
        internal static byte[] SmallestCompress(byte[] compressed, int stackLen)
        {
            if (!TryInflate(compressed, out var stack) || stack.Length != stackLen)
                return compressed;

            var recompressed = CompressStack(stack);
            return recompressed.Length > 0 && recompressed.Length < compressed.Length ? recompressed : compressed;
        }

        private static bool TryInflate(byte[] compressed, out byte[] output)
        {
            output = null;
            try
            {
                using var input = new MemoryStream(compressed);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                using var inflated = new MemoryStream();
                zlib.CopyTo(inflated);
                output = inflated.ToArray();
                return output.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        internal static byte[] CompressStack(byte[] stack)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(stack, 0, stack.Length);
            return output.ToArray();
        }

        internal static void Write(string path, byte[] compressed, int stackLen, byte[] bytecode)
        {
            using var writer = new BinaryWriter(File.Create(path));
            writer.Write((byte)'G');
            writer.Write((byte)'S');
            writer.Write((byte)'C');
            writer.Write((byte)0);
            writer.Write(compressed.Length);
            writer.Write(stackLen);
            writer.Write(bytecode.Length);
            writer.Write(compressed);
            writer.Write(bytecode);
        }

        internal static void Read(string path, out byte[] compressed, out int stackLen, out byte[] bytecode)
        {
            var data = File.ReadAllBytes(path);
            var offset = 0;
            if (data.Length >= 4 && data[0] == (byte)'G' && data[1] == (byte)'S' && data[2] == (byte)'C' && data[3] == 0)
                offset = 4;
            else
            {
                while (offset < data.Length && data[offset] != 0)
                    offset++;
                offset++;
            }

            if (offset + 12 > data.Length)
                throw new InvalidDataException("Invalid gscbin: " + path);

            var compressedLen = BitConverter.ToInt32(data, offset);
            stackLen = BitConverter.ToInt32(data, offset + 4);
            var bytecodeLen = BitConverter.ToInt32(data, offset + 8);
            offset += 12;
            if (compressedLen <= 0 || bytecodeLen <= 0 || offset + compressedLen + bytecodeLen > data.Length)
                throw new InvalidDataException("Invalid gscbin lengths: " + path);

            compressed = new byte[compressedLen];
            bytecode = new byte[bytecodeLen];
            Buffer.BlockCopy(data, offset, compressed, 0, compressedLen);
            Buffer.BlockCopy(data, offset + compressedLen, bytecode, 0, bytecodeLen);
        }
    }
}
