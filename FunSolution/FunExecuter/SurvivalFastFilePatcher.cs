using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FunExecuter
{
    internal static class SurvivalFastFilePatcher
    {
        private const string InjectMarker = "fun_hiho";
        private static readonly Regex WaveStartedNotify = new Regex(
            @"level\s+notify\s*\(\s*""wave_started""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static void PatchAndInstall(string gamePath, string toolsDir)
        {
            var fastFiles = FindPatchSurvivalFastFiles(gamePath);
            if (fastFiles.Count == 0)
                throw new FileNotFoundException("Could not find zone\\*\\patch_survival.ff under " + gamePath);

            var gscTool = Directory.EnumerateFiles(toolsDir, "gsc-tool.exe", SearchOption.AllDirectories).First();

            var workRoot = Path.Combine(Path.GetTempPath(), "FunExecuter", "ffwork");
            if (Directory.Exists(workRoot))
                Directory.Delete(workRoot, recursive: true);
            Directory.CreateDirectory(workRoot);

            foreach (var fastFile in fastFiles)
            {
                Console.WriteLine("Patching Survival FastFile: " + fastFile);
                var backup = EnsureVanillaBackup(fastFile);
                var built = BuildPatchedFastFile(backup, workRoot, gscTool);
                File.Copy(built, fastFile, overwrite: true);
                Console.WriteLine("Installed FastFile: " + fastFile);
            }
        }

        private static string EnsureVanillaBackup(string fastFile)
        {
            var dir = Path.GetDirectoryName(fastFile)!;
            var backup = Path.Combine(dir, "patch_survival.vanilla.ff");
            var oldBackup = fastFile + ".fun.bak";
            if (!File.Exists(backup))
            {
                if (File.Exists(oldBackup))
                    File.Copy(oldBackup, backup);
                else
                    File.Copy(fastFile, backup);
                Console.WriteLine("Backed up original to " + backup);
            }
            return backup;
        }

        private static List<string> FindPatchSurvivalFastFiles(string gamePath)
        {
            var zoneDir = Path.Combine(gamePath, "zone");
            if (!Directory.Exists(zoneDir))
                return new List<string>();

            return Directory.EnumerateFiles(zoneDir, "patch_survival.ff", SearchOption.AllDirectories)
                .OrderBy(p => p.IndexOf($"{Path.DirectorySeparatorChar}english{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildPatchedFastFile(string sourceFastFile, string workRoot, string gscTool)
        {
            var dumpDir = Path.Combine(workRoot, "dump");
            var outDir = Path.Combine(workRoot, "out");
            Directory.CreateDirectory(dumpDir);
            Directory.CreateDirectory(outDir);

            var isolatedFf = IsolateFastFile(sourceFastFile, workRoot);
            LogFastFileHeader(isolatedFf);

            Console.WriteLine("OpenAssetTools Unlinker crashes on this FastFile (access violation). Inflating and patching ScriptFiles in-place.");
            var fastFile = Iw5FastFile.Load(isolatedFf);
            var slots = fastFile.FindScriptSlots();
            Console.WriteLine("Found " + slots.Count + " ScriptFile buffer(s) in the inflated FastFile.");
            if (slots.Count == 0)
                throw new InvalidOperationException("No ScriptFile buffers were found after inflating patch_survival.ff.");

            var patchedAny = false;
            foreach (var slot in slots)
            {
                if (!Iw5FastFile.StackContains(slot.Stack, "wave_started"))
                    continue;

                var label = string.IsNullOrEmpty(slot.Name) ? ("script@" + slot.BufferOffset) : slot.Name;
                string decompiled;
                try
                {
                    decompiled = DecompileSlot(gscTool, dumpDir, label, slot);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Skipping " + label + " (decompile failed: " + ex.Message + ")");
                    continue;
                }

                var source = File.ReadAllText(decompiled, Encoding.UTF8);
                if (!WaveStartedNotify.IsMatch(source))
                {
                    Console.WriteLine("Skipping " + label + " (stack has wave_started but decompiled source has no notify).");
                    continue;
                }

                var patched = InjectHihoHook(source);
                if (patched == source)
                    Console.WriteLine("Already hooked: " + label);
                else
                {
                    File.WriteAllText(decompiled, patched, new UTF8Encoding(false));
                    Console.WriteLine("Hooked wave_started in: " + label);
                }

                var compiledBin = CompileGsc(gscTool, dumpDir, decompiled);
                GscBinFile.Read(compiledBin, out var compressed, out var stackLen, out var bytecode);
                Console.WriteLine(
                    "Compiled " + label + ": stack " + compressed.Length + "/" + slot.BufferLength +
                    " compressed, bytecode " + bytecode.Length + "/" + slot.BytecodeLength + ".");
                if (!fastFile.TryReplaceScript(slot, compressed, stackLen, bytecode, slots))
                {
                    Console.WriteLine("Skipping " + label + " (compiled script does not fit the FastFile slot).");
                    continue;
                }

                patchedAny = true;
                break;
            }

            if (!patchedAny)
                throw new InvalidOperationException("No Survival ScriptFile containing wave_started could be decompiled and patched.");

            var built = Path.Combine(outDir, "patch_survival.ff");
            fastFile.Save(built);
            Console.WriteLine("Wrote patched FastFile: " + built + " (" + new FileInfo(built).Length + " bytes).");
            return built;
        }

        private static string IsolateFastFile(string sourceFastFile, string workRoot)
        {
            var inputDir = Path.Combine(workRoot, "input");
            Directory.CreateDirectory(inputDir);
            var isolated = Path.Combine(inputDir, "patch_survival.ff");
            File.Copy(sourceFastFile, isolated, overwrite: true);
            Console.WriteLine("Isolated FastFile: " + isolated);
            return isolated;
        }

        private static void LogFastFileHeader(string path)
        {
            using var stream = File.OpenRead(path);
            var header = new byte[Math.Min(32, stream.Length)];
            var read = stream.Read(header, 0, header.Length);
            var magic = Encoding.ASCII.GetString(header, 0, Math.Min(8, read)).Replace('\0', ' ');
            Console.WriteLine("FastFile size: " + stream.Length + " bytes, magic: \"" + magic.Trim() + "\", header: " + BitConverter.ToString(header, 0, read));
        }

        private static string DecompileSlot(string gscTool, string dumpDir, string label, Iw5ScriptSlot slot)
        {
            var safeName = SanitizeFileName(label);
            var basePath = Path.Combine(dumpDir, safeName);
            var cgscPath = basePath + ".cgsc";
            File.WriteAllBytes(cgscPath, slot.Bytecode);
            File.WriteAllBytes(cgscPath + ".stack", slot.Stack);

            try
            {
                RunTool(gscTool, dumpDir, "-m", "decomp", "-g", "iw5", "-s", "pc", "-z", cgscPath);
                var fromCgsc = FindGscToolOutput(dumpDir, safeName, ".gsc");
                if (fromCgsc != null)
                    return fromCgsc;
            }
            catch (Exception ex)
            {
                Console.WriteLine("zonetool decomp failed for " + label + ": " + ex.Message);
            }

            var binPath = basePath + ".gscbin";
            GscBinFile.Write(binPath, GscBinFile.CompressStack(slot.Stack), slot.Stack.Length, slot.Bytecode);
            RunTool(gscTool, dumpDir, "-m", "decomp", "-g", "iw5", "-s", "pc", binPath);
            return FindGscToolOutput(dumpDir, safeName, ".gsc")
                ?? throw new FileNotFoundException("gsc-tool did not write " + safeName + ".gsc");
        }

        private static string CompileGsc(string gscTool, string dumpDir, string gscPath)
        {
            RunTool(gscTool, dumpDir, "-m", "comp", "-g", "iw5", "-s", "pc", gscPath);
            var name = Path.GetFileNameWithoutExtension(gscPath);
            return FindGscToolOutput(dumpDir, name, ".gscbin")
                ?? FindGscToolOutput(Path.GetDirectoryName(gscPath)!, name, ".gscbin")
                ?? throw new FileNotFoundException("gsc-tool did not write " + name + ".gscbin");
        }

        private static string FindGscToolOutput(string root, string baseName, string extension)
        {
            var fileName = baseName + extension;
            var candidates = new[]
            {
                Path.Combine(root, fileName),
                Path.Combine(root, "iw5", fileName),
                Path.Combine(root, "iw5", "pc", fileName)
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
        }

        internal static string InjectHihoHook(string source)
        {
            if (source.Contains(InjectMarker, StringComparison.Ordinal))
                return source;

            var match = WaveStartedNotify.Match(source);
            if (!match.Success)
                return source;

            var semicolon = source.IndexOf(';', match.Index);
            if (semicolon < 0)
                return source;

            var call = Environment.NewLine + "\tthread fun_hiho();" + Environment.NewLine;
            source = source.Insert(semicolon + 1, call);
            source += @"

fun_hiho()
{
	wait 10;
	iprintlnbold( ""HIHO christmas!"" );
}
";
            return source;
        }

        private static void RunTool(string fileName, string workingDirectory, params string[] arguments)
        {
            Console.WriteLine(Path.GetFileName(fileName) + " " + string.Join(" ", arguments.Select(Quote)));
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };
            foreach (var argument in arguments)
                start.ArgumentList.Add(argument);

            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Failed to start " + fileName);

            if (!process.WaitForExit(180000))
            {
                try { process.Kill(true); } catch { }
                throw new TimeoutException(Path.GetFileName(fileName) + " timed out.");
            }

            if (process.ExitCode != 0)
            {
                var hint = process.ExitCode == -1073741819
                    ? " (Windows access violation — the tool crashed. This is not an Administrator/permission error.)"
                    : "";
                throw new InvalidOperationException(Path.GetFileName(fileName) + " failed with exit code " + process.ExitCode + hint + ".");
            }
        }

        private static string SanitizeFileName(string value)
        {
            var chars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
                builder.Append(Array.IndexOf(chars, c) >= 0 ? '_' : c);
            return builder.Length == 0 ? "script" : builder.ToString();
        }

        private static string Quote(string value)
        {
            return value.IndexOfAny(new[] { ' ', '"' }) >= 0 ? "\"" + value + "\"" : value;
        }
    }
}
