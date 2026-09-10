using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FunExecuter
{
    internal static class GscIwdMode
    {
        internal static void Run(string[] args)
        {
            var gamePath = ResolveGamePath(args);
            var exePath = Path.Combine(gamePath, Constants.GAME_EXE_NAME);
            if (!File.Exists(exePath))
                throw new FileNotFoundException("Could not find " + Constants.GAME_EXE_NAME + " in " + gamePath);

            LogEmbeddedGscResources();

            var entries = LoadGscEntries();
            if (entries.Count == 0)
                throw new InvalidOperationException("No .gsc files found (embedded or Gsc folder).");

            foreach (var entry in entries)
                Console.WriteLine("GSC: " + entry.Path + " (" + entry.Data.Length + " bytes, UTF-8 no BOM)");

            StopRunningGame();

            Console.WriteLine("Original MW3 loads Survival GSC from patch_survival.ff, not from IWD/userraw.");
            var toolsDir = ToolBundles.ExtractToTemp();
            SurvivalFastFilePatcher.PatchAndInstall(gamePath, toolsDir);

            var userrawDir = Path.Combine(gamePath, "userraw");
            var mainDir = Path.Combine(gamePath, "main");
            Directory.CreateDirectory(userrawDir);
            Directory.CreateDirectory(mainDir);

            var iwdName = Constants.FUN_IWD_FILE_NAME;
            var stagingIwd = Path.Combine(Path.GetTempPath(), iwdName);
            IwdArchiveWriter.Write(stagingIwd, entries);
            CopyReplace(stagingIwd, Path.Combine(userrawDir, iwdName));
            CopyReplace(stagingIwd, Path.Combine(mainDir, iwdName));

            foreach (var entry in entries)
            {
                var loosePath = Path.Combine(userrawDir, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(loosePath)!);
                File.WriteAllBytes(loosePath, entry.Data);
            }

            try
            {
                File.Delete(stagingIwd);
            }
            catch (IOException)
            {
            }

            Console.WriteLine("Launching " + exePath);
            var start = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = gamePath,
                UseShellExecute = true
            };
            var launched = Process.Start(start);
            if (launched != null)
                Console.WriteLine("Launched PID " + launched.Id);
            Console.WriteLine("FunExecuter injected GSC into patch_survival.ff.");
            Console.WriteLine("Intermission between waves is 60 seconds (skip still starts the next wave immediately).");
            Console.WriteLine("Sentry minigun costs $3000, keeps its health, and up to 4 can be owned (2 per player in co-op). Buy another only after the current one is placed.");
            Console.WriteLine("Body armor soaks 1000 damage and can be bought again while remaining armor is still above 250.");
            Console.WriteLine("Each player can buy up to 4 riot shield squads.");
            Console.WriteLine("Start Survival; when wave 1 begins you should see: HIHO christmas!");
        }

        private static string ResolveGamePath(string[] args)
        {
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && Directory.Exists(args[0]))
                return Path.GetFullPath(args[0]);

            if (Directory.Exists(Constants.MW3_BASE_PATH))
                return Constants.MW3_BASE_PATH;

            throw new DirectoryNotFoundException("MW3 path not found: " + Constants.MW3_BASE_PATH);
        }

        private static List<IwdEntry> LoadGscEntries()
        {
            if (IsSingleFilePublish())
            {
                var embeddedOnly = LoadGscFromEmbeddedResources();
                if (embeddedOnly.Count == 0)
                    throw new InvalidOperationException("Single-file FunExecuter.exe has no embedded .gsc resources.");
                Console.WriteLine("GSC source: embedded in FunExecuter.exe (single-file)");
                return embeddedOnly;
            }

            var fromDisk = TryLoadGscFromDisk();
            if (fromDisk.Count > 0)
            {
                Console.WriteLine("GSC source: folder");
                return fromDisk;
            }

            var embedded = LoadGscFromEmbeddedResources();
            if (embedded.Count > 0)
                Console.WriteLine("GSC source: embedded in FunExecuter.exe");
            return embedded;
        }

        private static bool IsSingleFilePublish()
        {
#if SINGLE_FILE_PUBLISH
            return true;
#else
            return false;
#endif
        }

        private static void LogEmbeddedGscResources()
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(n => n.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Console.WriteLine("Embedded GSC resources: " + names.Length);
            foreach (var name in names)
                Console.WriteLine("  " + name);
        }

        private static List<IwdEntry> TryLoadGscFromDisk()
        {
            var entries = new List<IwdEntry>();
            var gscRoot = FindGscRoot();
            if (gscRoot == null)
                return entries;

            Console.WriteLine("GSC root: " + gscRoot);
            foreach (var file in Directory.EnumerateFiles(gscRoot, "*.gsc", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(gscRoot, file).Replace('\\', '/');
                entries.Add(new IwdEntry
                {
                    Path = relative,
                    Data = GscFileEncoder.ReadAndEncode(file)
                });
            }
            return entries;
        }

        private static string FindGscRoot()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Gsc"),
                Path.Combine(Directory.GetCurrentDirectory(), "Gsc"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Gsc")
            };

            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (Directory.Exists(full) && Directory.EnumerateFiles(full, "*.gsc", SearchOption.AllDirectories).Any())
                    return full;
            }

            return null;
        }

        private static List<IwdEntry> LoadGscFromEmbeddedResources()
        {
            var entries = new List<IwdEntry>();
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                if (!name.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = assembly.GetManifestResourceStream(name);
                if (stream == null)
                    continue;

                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                entries.Add(new IwdEntry
                {
                    Path = ResourceNameToIwdPath(name),
                    Data = GscFileEncoder.Encode(memory.ToArray())
                });
            }
            return entries;
        }

        private static string ResourceNameToIwdPath(string resourceName)
        {
            const string marker = ".Gsc.";
            var index = resourceName.IndexOf(marker, StringComparison.Ordinal);
            var rest = index >= 0 ? resourceName[(index + marker.Length)..] : resourceName;
            if (rest.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase))
                rest = rest[..^4].Replace('.', '/') + ".gsc";
            return rest.Replace('\\', '/');
        }

        private static void StopRunningGame()
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Constants.GAME_EXE_NAME));
            foreach (var process in processes)
            {
                Console.WriteLine("Stopping running " + process.ProcessName + " PID " + process.Id + " so IWD files can be replaced.");
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(15000);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private static void CopyReplace(string source, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
            Console.WriteLine("Installed IWD: " + destination);
        }
    }
}
