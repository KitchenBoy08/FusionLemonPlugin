using MelonLoader.Installer.Core;
using QuestPatcher.Axml;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Pipes;
using System.Reflection;

namespace BonelabFusionPatch
{
	public class BonelabFusionPlugin : Plugin
	{
		public override string Name => "Fusion Patch";

		private static Patcher _patcher = null!;
		private static IPatchLogger? _logger;

        public override bool Run(Patcher patcher)
		{
			_patcher = patcher;
			_logger = patcher.Logger;

			PatchForEOS(patcher.Info.OutputBaseApkPath);
			if (patcher.Info.OutputLibApkPath != null)
				PatchForEOS(patcher.Info.OutputLibApkPath);

            return true;
		}

		private static bool PatchForEOS(string apkPath)
		{
			_logger?.Log($"Patching {apkPath} for EOS...");

            ManifestEditor.PatchManifest(apkPath);
			_logger?.Log("Finished patching manifest.");
            SetupResourcesFolder(apkPath);
			_logger?.Log("Finished setting up resources folder.");
            DexEditor.AddDexFiles(apkPath);

            return true;
        }

		private static bool SetupResourcesFolder(string apkPath)
		{
            using FileStream zipStream = new(apkPath, FileMode.Open);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read | ZipArchiveMode.Update);

			archive.GetEntry("res")?.Delete();

            using Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BonelabFusionPatch.resources.zip");
            using ZipArchive resourceArchive = new(resourceStream!, ZipArchiveMode.Read);

            // Copy all entries from the res folder
            foreach (var entry in resourceArchive.Entries)
            {
                if (entry.FullName.StartsWith("res/") && !string.IsNullOrEmpty(entry.Name))
                {
                    var newEntry = archive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var newEntryStream = newEntry.Open();
                    entryStream.CopyTo(newEntryStream);
                }
            }

            return true;
        }
    }
}
