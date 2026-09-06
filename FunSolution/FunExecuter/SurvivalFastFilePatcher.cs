using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FunExecuter
{
    internal static class SurvivalFastFilePatcher
    {
        private const string HihoMarker = "fun_hiho_christmas";
        private const string G18Marker = "fun_g18_akimbo";
        private const string IntermissionMarker = "fun_intermission_seconds";
        private static readonly Regex WaveStartedNotify = new Regex(
            @"level\s+notify\s*\(\s*""wave_started""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex WeaponTableLoad = new Regex(
            @"\w+\s*\(\s*0\s*,\s*64\s*,\s*""weapon""\s*\)\s*;",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ArmoryIndexLookup = new Regex(
            @"return\s+tablelookup\s*\(\s*""sp/survival_armories\.csv""\s*,\s*0\s*,\s*(\w+)\s*,\s*1\s*\)\s*;",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // 1571 preload calls the armory ScriptFile (1557) as a far call during precache.
        private static readonly Regex ArmoryInitCall = new Regex(
            @"::\s*_id_3EBB\s*\(\s*\)\s*;",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ArmoryPrecacheMenu = new Regex(
            @"precachemenu\s*\(\s*""survival_armory_weapon""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SurvivalAllReadyTimeout = new Regex(
            @"waittill_any_timeout\s*\(\s*([^,\r\n]+?)\s*,\s*""survival_all_ready""\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SpecialOpsReadyHud = new Regex(
            @"(_id_132D\s*=\s*)30(\s*;)",
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

            var hihoSource = LoadGscSource("hiho_christmas.gsc");
            var g18Source = LoadGscSource("g18_akimbo.gsc");
            var intermissionSource = LoadGscSource("intermission.gsc");
            var patchedHiho = false;
            var patchedG18 = false;
            var patchedIntermission = false;
            var replacements = new List<(Iw5ScriptSlot Slot, byte[] Compressed, int StackLen, byte[] Bytecode, bool Hiho, bool G18, bool Intermission)>();

            foreach (var slot in slots)
            {
                var wantHiho = Iw5FastFile.StackContains(slot.Stack, "wave_started");
                var wantG18 = Iw5FastFile.StackContains(slot.Stack, "survival_armories")
                    || Iw5FastFile.StackContains(slot.Stack, "survival_armory_weapon")
                    || Iw5FastFile.StackContains(slot.Stack, "specops_ui_weaponstore");
                var wantIntermission = Iw5FastFile.StackContains(slot.Stack, "survival_all_ready");
                if (!wantHiho && !wantG18 && !wantIntermission)
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
                var patched = source;

                if (wantHiho && WaveStartedNotify.IsMatch(source))
                {
                    var hooked = InjectGscAfterWaveStarted(patched, hihoSource);
                    if (hooked == patched)
                        Console.WriteLine("Already hooked wave_started in: " + label);
                    else
                    {
                        Console.WriteLine("Injected hiho_christmas.gsc into: " + label);
                        patched = hooked;
                        patchedHiho = true;
                    }
                }

                if (!wantG18 && LooksLikeArmoryTarget(patched))
                    wantG18 = true;

                if (wantG18)
                {
                    var hooked = InjectG18Akimbo(patched, g18Source);
                    if (hooked.Contains(G18Marker, StringComparison.Ordinal) && patched.Contains(G18Marker, StringComparison.Ordinal))
                        Console.WriteLine("Already hooked G18 Akimbo in: " + label);
                    else if (!hooked.Contains(G18Marker, StringComparison.Ordinal))
                        Console.WriteLine("Could not find a G18 Akimbo hook site in: " + label);
                    else
                    {
                        Console.WriteLine("Injected g18_akimbo.gsc into: " + label);
                        patched = hooked;
                        patchedG18 = true;
                    }
                }

                if (!wantIntermission && SurvivalAllReadyTimeout.IsMatch(patched))
                    wantIntermission = true;

                if (wantIntermission)
                {
                    var hadMarker = patched.Contains(IntermissionMarker, StringComparison.Ordinal);
                    var hooked = InjectIntermission(patched, intermissionSource);
                    if (!hooked.Contains(IntermissionMarker, StringComparison.Ordinal))
                        Console.WriteLine("Could not find Survival intermission wait in: " + label);
                    else if (hadMarker)
                        Console.WriteLine("Already hooked 60s intermission in: " + label);
                    else
                    {
                        Console.WriteLine("Injected intermission.gsc into: " + label);
                        patched = hooked;
                        patchedIntermission = true;
                    }
                }

                if (patched == source)
                    continue;

                File.WriteAllText(decompiled, patched, new UTF8Encoding(false));
                var compiledBin = CompileGsc(gscTool, dumpDir, decompiled);
                GscBinFile.Read(compiledBin, out var compressed, out var stackLen, out var bytecode);
                Console.WriteLine(
                    "Compiled " + label + ": stack " + compressed.Length + "/" + slot.BufferLength +
                    " compressed, bytecode " + bytecode.Length + "/" + slot.BytecodeLength + ".");
                var injectedHiho = patched.Contains("fun_hiho_christmas", StringComparison.Ordinal);
                var injectedG18 = patched.Contains("fun_g18_akimbo", StringComparison.Ordinal);
                var injectedIntermission = patched.Contains(IntermissionMarker, StringComparison.Ordinal);
                replacements.Add((slot, compressed, stackLen, bytecode, injectedHiho, injectedG18, injectedIntermission));
            }

            patchedHiho = false;
            patchedG18 = false;
            patchedIntermission = false;
            foreach (var replacement in replacements.OrderByDescending(r => r.Slot.BufferOffset))
            {
                if (!fastFile.TryReplaceScript(replacement.Slot, replacement.Compressed, replacement.StackLen, replacement.Bytecode, slots))
                {
                    Console.WriteLine("Skipping script@" + replacement.Slot.BufferOffset + " (compiled script does not fit the FastFile slot).");
                    continue;
                }

                if (replacement.Hiho)
                    patchedHiho = true;
                if (replacement.G18)
                    patchedG18 = true;
                if (replacement.Intermission)
                    patchedIntermission = true;
            }

            if (!patchedHiho)
                throw new InvalidOperationException("No Survival ScriptFile containing wave_started could be decompiled and patched.");
            if (!patchedG18)
                throw new InvalidOperationException("No Survival armory ScriptFile could be decompiled and patched with g18_akimbo.gsc.");
            if (!patchedIntermission)
                throw new InvalidOperationException("No Survival ScriptFile containing survival_all_ready could be patched with 60s intermission.");

            var renamed = fastFile.ReplaceExactCString("WEAPON_GLOCK", "PUFF PUFF");
            Console.WriteLine(
                renamed > 0
                    ? "Armory menu name WEAPON_GLOCK -> PUFF PUFF (" + renamed + " string(s))."
                    : "WEAPON_GLOCK was not in the FastFile string pool; G18 slot still gives akimbo via GSC.");

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

        private static string InjectGscAfterWaveStarted(string source, string gscFile)
        {
            if (source.Contains(HihoMarker, StringComparison.Ordinal))
                return source;

            var match = WaveStartedNotify.Match(source);
            if (!match.Success)
                return source;

            var bodyStart = source.LastIndexOf('{', match.Index);
            if (bodyStart < 0)
                return source;

            source = source.Insert(bodyStart + 1, Environment.NewLine + "\tthread fun_hiho_christmas();" + Environment.NewLine);
            return AppendGsc(source, gscFile);
        }

        private static string InjectIntermission(string source, string gscFile)
        {
            if (source.Contains(IntermissionMarker, StringComparison.Ordinal))
                return source;

            var timeout = SurvivalAllReadyTimeout.Match(source);
            if (!timeout.Success)
                return source;

            var timeoutArg = timeout.Groups[1].Value.Trim();
            var searchStart = Math.Max(0, timeout.Index - 800);
            var window = source.Substring(searchStart, timeout.Index - searchStart);
            var durationVar = Regex.IsMatch(timeoutArg, @"^\w+$") ? timeoutArg : null;
            var assign = durationVar == null
                ? Match.Empty
                : Regex.Match(
                    window,
                    @"\b" + Regex.Escape(durationVar) + @"\s*=\s*\d+\s*;",
                    RegexOptions.RightToLeft);

            if (assign.Success)
            {
                var absIndex = searchStart + assign.Index;
                source = source.Remove(absIndex, assign.Length)
                    .Insert(absIndex, durationVar + " = fun_intermission_seconds();");
            }
            else
            {
                source = source.Remove(timeout.Index, timeout.Length)
                    .Insert(timeout.Index, "waittill_any_timeout( fun_intermission_seconds(), \"survival_all_ready\" )");
            }

            source = SpecialOpsReadyHud.Replace(source, "$1fun_intermission_seconds()$2");
            source = Regex.Replace(
                source,
                @"max\s*\(\s*(\w+)\s*,\s*30\s*\)(?=[\s\S]{0,160}?_id_132D)",
                "max( $1, fun_intermission_seconds() )",
                RegexOptions.IgnoreCase);

            return AppendGsc(source, gscFile);
        }

        private static bool LooksLikeArmoryTarget(string source)
        {
            return WeaponTableLoad.IsMatch(source)
                || ArmoryInitCall.IsMatch(source)
                || ArmoryPrecacheMenu.IsMatch(source);
        }

        private static string InjectG18Akimbo(string source, string gscFile)
        {
            if (source.Contains(G18Marker, StringComparison.Ordinal))
                return source;

            var tableLoad = WeaponTableLoad.Match(source);
            if (tableLoad.Success)
            {
                source = source.Insert(tableLoad.Index + tableLoad.Length, Environment.NewLine + "\tfun_g18_akimbo_register();");

                var lookup = ArmoryIndexLookup.Match(source);
                if (lookup.Success)
                {
                    var indexVar = lookup.Groups[1].Value;
                    var wrap =
                        "var_fun_g18 = fun_g18_akimbo_item_name( " + indexVar + " );" + Environment.NewLine +
                        "\tif ( var_fun_g18 != \"\" )" + Environment.NewLine +
                        "\t\treturn var_fun_g18;" + Environment.NewLine + "\t";
                    source = source.Insert(lookup.Index, wrap);
                }

                return AppendGsc(source, gscFile);
            }

            // ScriptFile 1557 is often missing from the zlib scan. 1571 still calls
            // _id_3EBB() during preload; register G18 immediately after that returns.
            var armoryInit = ArmoryInitCall.Match(source);
            if (!armoryInit.Success)
                return source;

            source = source.Insert(
                armoryInit.Index + armoryInit.Length,
                Environment.NewLine + "\tfun_g18_akimbo_register();");
            return AppendGsc(source, gscFile);
        }

        private static readonly Regex EntryPointFunction = new Regex(
            @"(^|\n)(init|main)\s*\(\s*\)\s*\{",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string AppendGsc(string source, string gscFile)
        {
            gscFile = StripEntryPointFunctions(gscFile);
            if (string.IsNullOrWhiteSpace(gscFile))
                return source;

            if (!source.EndsWith("\n", StringComparison.Ordinal))
                source += Environment.NewLine;

            return source + Environment.NewLine + gscFile + Environment.NewLine;
        }

        private static string StripEntryPointFunctions(string gscFile)
        {
            var source = gscFile;
            Match match;
            while ((match = EntryPointFunction.Match(source)).Success)
            {
                var brace = source.IndexOf('{', match.Index);
                var end = FindMatchingBrace(source, brace);
                if (brace < 0 || end < 0)
                    break;

                var start = match.Index + (match.Groups[1].Length > 0 ? match.Groups[1].Length : 0);
                source = source.Remove(start, end - start + 1);
            }

            return source.Trim();
        }

        private static int FindMatchingBrace(string source, int openBrace)
        {
            if (openBrace < 0 || openBrace >= source.Length || source[openBrace] != '{')
                return -1;

            var depth = 0;
            for (var i = openBrace; i < source.Length; i++)
            {
                var c = source[i];
                if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static string LoadGscSource(string fileName)
        {
#if !SINGLE_FILE_PUBLISH
            var fromDisk = TryLoadGscFromDisk(fileName);
            if (fromDisk != null)
                return fromDisk;
#endif
            Console.WriteLine("Loading GSC from embedded resource: " + fileName);

            var assembly = Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = assembly.GetManifestResourceStream(name);
                if (stream == null)
                    continue;

                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var text = reader.ReadToEnd().Trim();
                if (text.Length > 0)
                    return text;
            }

            throw new FileNotFoundException("Could not load GSC source: " + fileName);
        }

#if !SINGLE_FILE_PUBLISH
        private static string TryLoadGscFromDisk(string fileName)
        {
            var roots = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Gsc"),
                Path.Combine(Directory.GetCurrentDirectory(), "Gsc"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Gsc")
            };

            foreach (var root in roots)
            {
                var fullRoot = Path.GetFullPath(root);
                if (!Directory.Exists(fullRoot))
                    continue;

                var match = Directory.EnumerateFiles(fullRoot, fileName, SearchOption.AllDirectories).FirstOrDefault();
                if (match != null)
                    return File.ReadAllText(match, Encoding.UTF8).Trim();
            }

            return null;
        }
#endif

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
