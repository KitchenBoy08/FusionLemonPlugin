using System.IO.Compression;

namespace BonelabFusionPatch;

internal class ResourceEditor
{
    internal static bool PatchResources(string apkPath)
    {
        using FileStream zipStream = new(apkPath, FileMode.Open);
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Read | ZipArchiveMode.Update);


        ZipArchiveEntry? resourcesEntry = archive.GetEntry("resources.arsc");
        if (resourcesEntry != null)
            resourcesEntry.Delete();

        ZipArchiveEntry? resEntry = archive.GetEntry("res/");
        if (resEntry != null)
            resEntry.Delete();

        using Stream? resourceStream = typeof(ResourceEditor).Assembly.GetManifestResourceStream("BonelabFusionPatch.resources.zip");
        if (resourceStream == null)
            return false;

        using Stream? resourceArscStream = typeof(ResourceEditor).Assembly.GetManifestResourceStream("BonelabFusionPatch.resources.arsc");
        if (resourceArscStream == null)
            return false;

        using ZipArchive resourceArchive = new(resourceStream, ZipArchiveMode.Read);

        ZipArchiveEntry newResourcesEntry = archive.CreateEntry("resources.arsc", CompressionLevel.NoCompression);
        using Stream newResourcesStream = newResourcesEntry.Open();
        resourceArscStream.CopyTo(newResourcesStream);

        foreach (ZipArchiveEntry entry in resourceArchive.Entries)
        {
            if (entry.FullName.StartsWith("res/") && !string.IsNullOrEmpty(entry.Name))
            {
                ZipArchiveEntry newEntry = archive.CreateEntry(entry.FullName);
                using Stream sourceStream = entry.Open();
                using Stream destStream = newEntry.Open();
                sourceStream.CopyTo(destStream);
            }
        }

        return true;
    }
}