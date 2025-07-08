using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BonelabFusionPatch
{
    internal class DexEditor
    {
        internal static bool AddDexFiles(string apkPath)
        {
            using FileStream zipStream = new(apkPath, FileMode.Open);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read | ZipArchiveMode.Update);

            RenameEntry(archive, "classes.dex", "classes3.dex");

            using Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BonelabFusionPatch.resources.zip");
            using ZipArchive resourceArchive = new(resourceStream!, ZipArchiveMode.Read);

            // Copy all entries from the res folder
            foreach (var entry in resourceArchive.Entries)
            {
                if (entry.FullName.StartsWith("classes") && !string.IsNullOrEmpty(entry.Name))
                {
                    var newEntry = archive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var newEntryStream = newEntry.Open();
                    entryStream.CopyTo(newEntryStream);
                }
            }

            return true;
        }

        private static void RenameEntry(ZipArchive archive, string oldName, string newName)
        {
            var entry = archive.GetEntry(oldName);
            if (entry == null)
                throw new FileNotFoundException($"Entry '{oldName}' not found in archive.");

            // Create new entry and copy data
            var newEntry = archive.CreateEntry(newName, CompressionLevel.Optimal);
            using (var oldStream = entry.Open())
            using (var newStream = newEntry.Open())
            {
                oldStream.CopyTo(newStream);
            }

            // Delete the old entry
            entry.Delete();
        }
    }
}
