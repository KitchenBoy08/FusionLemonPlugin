using MelonLoader.Installer.Core;

namespace BonelabFusionPatch;

public class BonelabFusionPlugin : Plugin
{
    public override string Name => "Fusion Patch";
    public override string[] CompatiblePackages => ["com.StressLevelZero.BONELAB"];

    private static Patcher _patcher = null!;
    private static IPatchLogger? _logger;

    public override bool Run(Patcher patcher)
    {
        _patcher = patcher;
        _logger = patcher.Logger;

        string title = "====================\nFusion EOS Patcher\n====================";
        _logger?.Log(title);

        PatchForEOS(patcher.Info.OutputBaseApkPath);
        if (patcher.Info.OutputLibApkPath != null)
            PatchForEOS(patcher.Info.OutputLibApkPath);

        return true;
    }

    private static bool PatchForEOS(string apkPath)
    {
        _logger?.Log($"Applying EOS patch to {apkPath}...");

        ManifestEditor.PatchManifest(apkPath);
        _logger?.Log("Finished patching manifest.");
        ResourceEditor.PatchResources(apkPath);
        _logger?.Log("Finished setting up resources.");
        DexEditor.AddDexFiles(apkPath);
        _logger?.Log("Finished adding dex files.");

        return true;
    }
}
