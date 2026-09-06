using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace FunExecuter
{
    internal static class ToolBundles
    {
        internal static string ExtractToTemp()
        {
            var dest = Path.Combine(Path.GetTempPath(), "FunExecuter", "tools");
            Directory.CreateDirectory(dest);

            ExtractZipResource("gsc-tool-windows-x64.zip", dest);

            var gscTool = Directory.EnumerateFiles(dest, "gsc-tool.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (gscTool == null)
                throw new FileNotFoundException("Failed to extract gsc-tool.exe.");

            Console.WriteLine("Tools: " + dest);
            return dest;
        }

        private static void ExtractZipResource(string zipFileName, string dest)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(zipFileName, StringComparison.OrdinalIgnoreCase));
            if (resource == null)
                throw new FileNotFoundException("Embedded tool archive not found: " + zipFileName);

            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("Could not open embedded archive: " + resource);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            zip.ExtractToDirectory(dest, overwriteFiles: true);
        }
    }
}
